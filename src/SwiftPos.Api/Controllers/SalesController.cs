using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftPos.Api.Security;
using SwiftPos.Infrastructure.Persistence;

namespace SwiftPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("sales")]
public sealed class SalesController(
    SwiftPosDbContext dbContext,
    CurrentUserContext currentUserContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SalesListResponse>>> ListSales(
        CancellationToken cancellationToken)
    {
        var currentUser = currentUserContext.GetRequired();
        var query = dbContext.Sales
            .AsNoTracking()
            .Where(sale => sale.TenantId == currentUser.TenantId);

        if (currentUser.StoreId is not null)
        {
            query = query.Where(sale => sale.StoreId == currentUser.StoreId.Value);
        }

        var sales = await query
            .OrderByDescending(sale => sale.CompletedAt)
            .Select(sale => new SalesListResponse(
                sale.Id,
                sale.OrderId,
                sale.StoreId,
                sale.Status,
                sale.Subtotal,
                sale.TaxTotal,
                sale.Total,
                sale.CompletedAt))
            .ToListAsync(cancellationToken);

        return Ok(sales);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDetailResponse>> GetSale(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUser = currentUserContext.GetRequired();
        var saleQuery = dbContext.Sales
            .AsNoTracking()
            .Where(sale => sale.Id == id && sale.TenantId == currentUser.TenantId);

        if (currentUser.StoreId is not null)
        {
            saleQuery = saleQuery.Where(sale => sale.StoreId == currentUser.StoreId.Value);
        }

        var sale = await saleQuery.FirstOrDefaultAsync(cancellationToken);
        if (sale is null)
        {
            return NotFound();
        }

        var items = await dbContext.SaleItems
            .AsNoTracking()
            .Where(item => item.TenantId == currentUser.TenantId && item.SaleId == sale.Id)
            .OrderBy(item => item.CreatedAt)
            .Select(item => new SaleItemResponse(
                item.Id,
                item.ProductId,
                item.ProductNameSnapshot,
                item.ProductSkuSnapshot,
                item.Quantity,
                item.UnitPrice,
                item.TaxRate,
                item.Subtotal,
                item.TaxTotal,
                item.Total))
            .ToListAsync(cancellationToken);

        var payments = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.TenantId == currentUser.TenantId && payment.SaleId == sale.Id)
            .OrderBy(payment => payment.PaidAt)
            .Select(payment => new PaymentResponse(
                payment.Id,
                payment.Method,
                payment.Status,
                payment.Amount,
                payment.Reference,
                payment.PaidAt))
            .ToListAsync(cancellationToken);

        return Ok(new SaleDetailResponse(
            sale.Id,
            sale.OrderId,
            sale.StoreId,
            sale.Status,
            sale.Subtotal,
            sale.TaxTotal,
            sale.Total,
            sale.CompletedAt,
            items,
            payments));
    }
}

public sealed record SalesListResponse(
    Guid Id,
    Guid OrderId,
    Guid StoreId,
    string Status,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    DateTimeOffset CompletedAt);

public sealed record SaleDetailResponse(
    Guid Id,
    Guid OrderId,
    Guid StoreId,
    string Status,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    DateTimeOffset CompletedAt,
    IReadOnlyCollection<SaleItemResponse> Items,
    IReadOnlyCollection<PaymentResponse> Payments);

public sealed record SaleItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total);
