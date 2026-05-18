using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftPos.Api.Security;
using SwiftPos.Domain.Pos;
using SwiftPos.Infrastructure.Persistence;

namespace SwiftPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("pos")]
public sealed class PosController(
    SwiftPosDbContext dbContext,
    CurrentUserContext currentUserContext) : ControllerBase
{
    [HttpPost("orders")]
    public async Task<ActionResult<OrderResponse>> CreateOrder(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateOrderRequest(request);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var currentUser = currentUserContext.GetRequired();
        if (currentUser.StoreId is null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Store context is required.",
                Detail = "POS orders require an authenticated user with a store claim."
            });
        }

        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            TenantId = currentUser.TenantId,
            StoreId = currentUser.StoreId.Value,
            CreatedByUserId = currentUser.UserId,
            Mode = request.Mode.Trim().ToUpperInvariant(),
            Status = OrderStatuses.Created,
            Notes = NormalizeOptional(request.Notes),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(CreateOrder), new { id = order.Id }, ToOrderResponse(order, []));
    }

    [HttpPost("orders/{id:guid}/items")]
    public async Task<ActionResult<OrderResponse>> AddItem(
        Guid id,
        OrderItemUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateOrderItemRequest(request);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var currentUser = currentUserContext.GetRequired();
        var order = await GetEditableOrder(id, currentUser.TenantId, currentUser.StoreId, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.ProductId
                    && candidate.TenantId == currentUser.TenantId,
                cancellationToken);

        if (product is null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid product.",
                Detail = "The selected product does not exist for the current tenant."
            });
        }

        if (!product.IsActive)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product is inactive.",
                Detail = "Inactive products cannot be added to POS orders."
            });
        }

        var totals = PosTotals.CalculateLine(product.Price, product.TaxRate, request.Quantity);
        var now = DateTimeOffset.UtcNow;
        dbContext.OrderItems.Add(new OrderItem
        {
            TenantId = currentUser.TenantId,
            OrderId = order.Id,
            ProductId = product.Id,
            ProductNameSnapshot = product.Name,
            ProductSkuSnapshot = product.Sku,
            Quantity = request.Quantity,
            UnitPrice = product.Price,
            TaxRate = product.TaxRate,
            Subtotal = totals.Subtotal,
            TaxTotal = totals.TaxTotal,
            Total = totals.Total,
            Notes = NormalizeOptional(request.Notes),
            CreatedAt = now,
            UpdatedAt = now
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(await RecalculateAndRespond(order, cancellationToken));
    }

    [HttpPut("orders/{id:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<OrderResponse>> UpdateItem(
        Guid id,
        Guid itemId,
        OrderItemUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Order item quantity is invalid.",
                Detail = "Quantity must be greater than zero."
            });
        }

        if (request.Notes?.Trim().Length > 500)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Order item notes are too long.",
                Detail = "Order item notes cannot exceed 500 characters."
            });
        }

        var currentUser = currentUserContext.GetRequired();
        var order = await GetEditableOrder(id, currentUser.TenantId, currentUser.StoreId, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var item = await dbContext.OrderItems
            .AsTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == itemId
                    && candidate.OrderId == order.Id
                    && candidate.TenantId == currentUser.TenantId,
                cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        var totals = PosTotals.CalculateLine(item.UnitPrice, item.TaxRate, request.Quantity);
        item.Quantity = request.Quantity;
        item.Subtotal = totals.Subtotal;
        item.TaxTotal = totals.TaxTotal;
        item.Total = totals.Total;
        item.Notes = NormalizeOptional(request.Notes);
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(await RecalculateAndRespond(order, cancellationToken));
    }

    [HttpDelete("orders/{id:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<OrderResponse>> DeleteItem(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var currentUser = currentUserContext.GetRequired();
        var order = await GetEditableOrder(id, currentUser.TenantId, currentUser.StoreId, cancellationToken);
        if (order is null)
        {
            return NotFound();
        }

        var item = await dbContext.OrderItems
            .AsTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == itemId
                    && candidate.OrderId == order.Id
                    && candidate.TenantId == currentUser.TenantId,
                cancellationToken);

        if (item is null)
        {
            return NotFound();
        }

        dbContext.OrderItems.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await RecalculateAndRespond(order, cancellationToken));
    }

    [HttpPost("orders/{id:guid}/checkout")]
    public async Task<ActionResult<CheckoutResponse>> Checkout(
        Guid id,
        CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateCheckoutRequest(request);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var currentUser = currentUserContext.GetRequired();
        if (currentUser.StoreId is null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Store context is required.",
                Detail = "POS checkout requires an authenticated user with a store claim."
            });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var order = await dbContext.Orders
            .AsTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == id
                    && candidate.TenantId == currentUser.TenantId
                    && candidate.StoreId == currentUser.StoreId.Value,
                cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        if (order.Status != OrderStatuses.Created)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Order cannot be checked out.",
                Detail = "Only created orders can be checked out."
            });
        }

        var existingSale = await dbContext.Sales
            .AsNoTracking()
            .AnyAsync(
                sale => sale.TenantId == currentUser.TenantId && sale.OrderId == order.Id,
                cancellationToken);

        if (existingSale)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Order already has a sale.",
                Detail = "A checked out order cannot be charged again."
            });
        }

        var items = await dbContext.OrderItems
            .AsNoTracking()
            .Where(item => item.TenantId == currentUser.TenantId && item.OrderId == order.Id)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Order has no items.",
                Detail = "Checkout requires at least one order item."
            });
        }

        var totals = PosTotals.CalculateOrder(items.Select(item => new PosLineTotals(
            item.Subtotal,
            item.TaxTotal,
            item.Total)));

        if (totals.Total <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Order total is invalid.",
                Detail = "Checkout requires an order total greater than zero."
            });
        }

        var paymentAmount = request.Amount ?? totals.Total;
        if (paymentAmount != totals.Total)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Payment amount must equal order total.",
                Detail = "The MVP supports one full payment per checkout."
            });
        }

        var now = DateTimeOffset.UtcNow;
        order.Status = OrderStatuses.Paid;
        order.Subtotal = totals.Subtotal;
        order.TaxTotal = totals.TaxTotal;
        order.Total = totals.Total;
        order.UpdatedAt = now;

        var sale = new Sale
        {
            TenantId = currentUser.TenantId,
            StoreId = currentUser.StoreId.Value,
            OrderId = order.Id,
            SoldByUserId = currentUser.UserId,
            CashRegisterSessionId = order.CashRegisterSessionId,
            Status = SaleStatuses.Completed,
            Subtotal = totals.Subtotal,
            TaxTotal = totals.TaxTotal,
            Total = totals.Total,
            CompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Sales.Add(sale);

        foreach (var item in items)
        {
            dbContext.SaleItems.Add(new SaleItem
            {
                TenantId = currentUser.TenantId,
                SaleId = sale.Id,
                ProductId = item.ProductId,
                ProductNameSnapshot = item.ProductNameSnapshot,
                ProductSkuSnapshot = item.ProductSkuSnapshot,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TaxRate = item.TaxRate,
                Subtotal = item.Subtotal,
                TaxTotal = item.TaxTotal,
                Total = item.Total,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        var payment = new Payment
        {
            TenantId = currentUser.TenantId,
            StoreId = currentUser.StoreId.Value,
            SaleId = sale.Id,
            Method = request.PaymentMethod.Trim().ToUpperInvariant(),
            Status = PaymentStatuses.Completed,
            Amount = paymentAmount,
            Reference = NormalizeOptional(request.Reference),
            PaidAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Payments.Add(payment);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = new CheckoutResponse(
            ToOrderResponse(order, items.Select(ToOrderItemResponse).ToList()),
            new SaleResponse(sale.Id, sale.OrderId, sale.Status, sale.Subtotal, sale.TaxTotal, sale.Total, sale.CompletedAt),
            new PaymentResponse(payment.Id, payment.Method, payment.Status, payment.Amount, payment.Reference, payment.PaidAt));

        return Created($"/sales/{sale.Id}", response);
    }

    private async Task<Order?> GetEditableOrder(
        Guid id,
        Guid tenantId,
        Guid? storeId,
        CancellationToken cancellationToken)
    {
        if (storeId is null)
        {
            return null;
        }

        return await dbContext.Orders
            .AsTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == id
                    && candidate.TenantId == tenantId
                    && candidate.StoreId == storeId.Value
                    && candidate.Status == OrderStatuses.Created,
                cancellationToken);
    }

    private async Task<OrderResponse> RecalculateAndRespond(Order order, CancellationToken cancellationToken)
    {
        var items = await dbContext.OrderItems
            .AsNoTracking()
            .Where(item => item.TenantId == order.TenantId && item.OrderId == order.Id)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var totals = PosTotals.CalculateOrder(items.Select(item => new PosLineTotals(
            item.Subtotal,
            item.TaxTotal,
            item.Total)));

        order.Subtotal = totals.Subtotal;
        order.TaxTotal = totals.TaxTotal;
        order.Total = totals.Total;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToOrderResponse(order, items.Select(ToOrderItemResponse).ToList());
    }

    private ActionResult? ValidateOrderRequest(CreateOrderRequest request)
    {
        var mode = request.Mode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(mode) || !OrderModes.IsValid(mode))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Order mode is invalid.",
                Detail = "Order mode must be DINE_IN, TAKEAWAY, or PICK_GO."
            });
        }

        if (request.Notes?.Trim().Length > 500)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Order notes are too long.",
                Detail = "Order notes cannot exceed 500 characters."
            });
        }

        return null;
    }

    private ActionResult? ValidateOrderItemRequest(OrderItemUpsertRequest request)
    {
        if (request.ProductId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product is required."
            });
        }

        if (request.Quantity <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Order item quantity is invalid.",
                Detail = "Quantity must be greater than zero."
            });
        }

        if (request.Notes?.Trim().Length > 500)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Order item notes are too long.",
                Detail = "Order item notes cannot exceed 500 characters."
            });
        }

        return null;
    }

    private ActionResult? ValidateCheckoutRequest(CheckoutRequest request)
    {
        var paymentMethod = request.PaymentMethod?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(paymentMethod) || !PaymentMethods.IsValid(paymentMethod))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Payment method is invalid.",
                Detail = "Payment method must be CASH or CARD_MANUAL."
            });
        }

        if (request.Amount is <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Payment amount is invalid.",
                Detail = "Payment amount must be greater than zero."
            });
        }

        if (request.Reference?.Trim().Length > 160)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Payment reference is too long.",
                Detail = "Payment reference cannot exceed 160 characters."
            });
        }

        return null;
    }

    private static OrderResponse ToOrderResponse(Order order, IReadOnlyCollection<OrderItemResponse> items)
    {
        return new OrderResponse(
            order.Id,
            order.StoreId,
            order.Mode,
            order.Status,
            order.Notes,
            order.Subtotal,
            order.TaxTotal,
            order.Total,
            order.CreatedAt,
            order.UpdatedAt,
            items);
    }

    private static OrderItemResponse ToOrderItemResponse(OrderItem item)
    {
        return new OrderItemResponse(
            item.Id,
            item.ProductId,
            item.ProductNameSnapshot,
            item.ProductSkuSnapshot,
            item.Quantity,
            item.UnitPrice,
            item.TaxRate,
            item.Subtotal,
            item.TaxTotal,
            item.Total,
            item.Notes);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed record CreateOrderRequest(string Mode, string? Notes);

public sealed record OrderItemUpsertRequest(Guid ProductId, int Quantity, string? Notes);

public sealed record OrderItemUpdateRequest(int Quantity, string? Notes);

public sealed record CheckoutRequest(string PaymentMethod, decimal? Amount, string? Reference);

public sealed record OrderResponse(
    Guid Id,
    Guid StoreId,
    string Mode,
    string Status,
    string? Notes,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<OrderItemResponse> Items);

public sealed record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    string? Notes);

public sealed record CheckoutResponse(
    OrderResponse Order,
    SaleResponse Sale,
    PaymentResponse Payment);

public sealed record SaleResponse(
    Guid Id,
    Guid OrderId,
    string Status,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    DateTimeOffset CompletedAt);

public sealed record PaymentResponse(
    Guid Id,
    string Method,
    string Status,
    decimal Amount,
    string? Reference,
    DateTimeOffset PaidAt);
