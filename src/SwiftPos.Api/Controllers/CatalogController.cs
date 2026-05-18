using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftPos.Api.Security;
using SwiftPos.Domain.Catalog;
using SwiftPos.Domain.Users;
using SwiftPos.Infrastructure.Persistence;

namespace SwiftPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("catalog")]
public sealed class CatalogController(
    SwiftPosDbContext dbContext,
    CurrentUserContext currentUserContext) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> ListCategories(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var currentUser = currentUserContext.GetRequired();
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.TenantId == currentUser.TenantId)
            .Where(category => includeInactive || category.IsActive)
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [Authorize(Roles = RoleCodes.Owner + "," + RoleCodes.Admin)]
    [HttpPost("categories")]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(
        CategoryUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateCategory(request);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var currentUser = currentUserContext.GetRequired();
        var normalizedName = request.Name.Trim();
        var exists = await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                category => category.TenantId == currentUser.TenantId
                    && category.Name.ToLower() == normalizedName.ToLowerInvariant(),
                cancellationToken);

        if (exists)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Category name already exists.",
                Detail = "Category names must be unique within the current tenant."
            });
        }

        var now = DateTimeOffset.UtcNow;
        var category = new Category
        {
            TenantId = currentUser.TenantId,
            Name = normalizedName,
            Description = NormalizeOptional(request.Description),
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToCategoryResponse(category);
        return CreatedAtAction(nameof(ListCategories), new { id = category.Id }, response);
    }

    [Authorize(Roles = RoleCodes.Owner + "," + RoleCodes.Admin)]
    [HttpPut("categories/{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(
        Guid id,
        CategoryUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateCategory(request);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var currentUser = currentUserContext.GetRequired();
        var category = await dbContext.Categories
            .AsTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == id && candidate.TenantId == currentUser.TenantId,
                cancellationToken);

        if (category is null)
        {
            return NotFound();
        }

        var normalizedName = request.Name.Trim();
        var exists = await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                candidate => candidate.Id != id
                    && candidate.TenantId == currentUser.TenantId
                    && candidate.Name.ToLower() == normalizedName.ToLowerInvariant(),
                cancellationToken);

        if (exists)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Category name already exists.",
                Detail = "Category names must be unique within the current tenant."
            });
        }

        category.Name = normalizedName;
        category.Description = NormalizeOptional(request.Description);
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToCategoryResponse(category));
    }

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyCollection<ProductResponse>>> ListProducts(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var currentUser = currentUserContext.GetRequired();
        var query =
            from product in dbContext.Products.AsNoTracking()
            join category in dbContext.Categories.AsNoTracking() on product.CategoryId equals category.Id
            where product.TenantId == currentUser.TenantId
            select new { product, category };

        if (!includeInactive)
        {
            query = query.Where(row => row.product.IsActive);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(row => row.product.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(row =>
                row.product.Name.ToLower().Contains(normalizedSearch)
                || row.product.Sku.ToLower().Contains(normalizedSearch)
                || (row.product.Barcode != null && row.product.Barcode.ToLower().Contains(normalizedSearch)));
        }

        var products = await query
            .OrderBy(row => row.product.Name)
            .Select(row => new ProductResponse(
                row.product.Id,
                row.product.CategoryId,
                row.category.Name,
                row.product.Sku,
                row.product.Barcode,
                row.product.Name,
                row.product.Description,
                row.product.Price,
                row.product.TaxRate,
                row.product.Cost,
                row.product.IsActive,
                row.product.TrackStock,
                row.product.Stock))
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    [Authorize(Roles = RoleCodes.Owner + "," + RoleCodes.Admin)]
    [HttpPost("products")]
    public async Task<ActionResult<ProductResponse>> CreateProduct(
        ProductUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateProduct(request);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var currentUser = currentUserContext.GetRequired();
        var category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.CategoryId && candidate.TenantId == currentUser.TenantId,
                cancellationToken);

        if (category is null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid category.",
                Detail = "The selected category does not exist for the current tenant."
            });
        }

        var normalizedSku = request.Sku.Trim();
        var skuExists = await dbContext.Products
            .AsNoTracking()
            .AnyAsync(
                product => product.TenantId == currentUser.TenantId
                    && product.Sku.ToLower() == normalizedSku.ToLowerInvariant(),
                cancellationToken);

        if (skuExists)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Product SKU already exists.",
                Detail = "Product SKUs must be unique within the current tenant."
            });
        }

        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            TenantId = currentUser.TenantId,
            CategoryId = request.CategoryId,
            Sku = normalizedSku,
            Barcode = NormalizeOptional(request.Barcode),
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            Price = request.Price,
            TaxRate = request.TaxRate,
            Cost = request.Cost,
            IsActive = request.IsActive,
            TrackStock = request.TrackStock,
            Stock = request.Stock,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToProductResponse(product, category.Name);
        return CreatedAtAction(nameof(ListProducts), new { id = product.Id }, response);
    }

    [Authorize(Roles = RoleCodes.Owner + "," + RoleCodes.Admin)]
    [HttpPut("products/{id:guid}")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(
        Guid id,
        ProductUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateProduct(request);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var currentUser = currentUserContext.GetRequired();
        var product = await dbContext.Products
            .AsTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == id && candidate.TenantId == currentUser.TenantId,
                cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        var category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.CategoryId && candidate.TenantId == currentUser.TenantId,
                cancellationToken);

        if (category is null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid category.",
                Detail = "The selected category does not exist for the current tenant."
            });
        }

        var normalizedSku = request.Sku.Trim();
        var skuExists = await dbContext.Products
            .AsNoTracking()
            .AnyAsync(
                candidate => candidate.Id != id
                    && candidate.TenantId == currentUser.TenantId
                    && candidate.Sku.ToLower() == normalizedSku.ToLowerInvariant(),
                cancellationToken);

        if (skuExists)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Product SKU already exists.",
                Detail = "Product SKUs must be unique within the current tenant."
            });
        }

        product.CategoryId = request.CategoryId;
        product.Sku = normalizedSku;
        product.Barcode = NormalizeOptional(request.Barcode);
        product.Name = request.Name.Trim();
        product.Description = NormalizeOptional(request.Description);
        product.Price = request.Price;
        product.TaxRate = request.TaxRate;
        product.Cost = request.Cost;
        product.IsActive = request.IsActive;
        product.TrackStock = request.TrackStock;
        product.Stock = request.Stock;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToProductResponse(product, category.Name));
    }

    [Authorize(Roles = RoleCodes.Owner + "," + RoleCodes.Admin)]
    [HttpDelete("products/{id:guid}")]
    public async Task<IActionResult> DeactivateProduct(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = currentUserContext.GetRequired();
        var product = await dbContext.Products
            .AsTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == id && candidate.TenantId == currentUser.TenantId,
                cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        product.IsActive = false;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private ActionResult? ValidateCategory(CategoryUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Category name is required."
            });
        }

        if (request.Name.Trim().Length > 120)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Category name is too long.",
                Detail = "Category names cannot exceed 120 characters."
            });
        }

        if (request.Description?.Trim().Length > 320)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Category description is too long.",
                Detail = "Category descriptions cannot exceed 320 characters."
            });
        }

        return null;
    }

    private ActionResult? ValidateProduct(ProductUpsertRequest request)
    {
        if (request.CategoryId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product category is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product SKU is required."
            });
        }

        if (request.Sku.Trim().Length > 80)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product SKU is too long.",
                Detail = "Product SKUs cannot exceed 80 characters."
            });
        }

        if (request.Barcode?.Trim().Length > 80)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product barcode is too long.",
                Detail = "Product barcodes cannot exceed 80 characters."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product name is required."
            });
        }

        if (request.Name.Trim().Length > 180)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product name is too long.",
                Detail = "Product names cannot exceed 180 characters."
            });
        }

        if (request.Description?.Trim().Length > 500)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product description is too long.",
                Detail = "Product descriptions cannot exceed 500 characters."
            });
        }

        if (request.Price < 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product price cannot be negative."
            });
        }

        if (request.TaxRate is < 0 or > 1)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product tax rate is invalid.",
                Detail = "Tax rate must be between 0 and 1."
            });
        }

        if (request.Cost < 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product cost cannot be negative."
            });
        }

        if (request.Stock < 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Product stock cannot be negative."
            });
        }

        return null;
    }

    private static CategoryResponse ToCategoryResponse(Category category)
    {
        return new CategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.IsActive);
    }

    private static ProductResponse ToProductResponse(Product product, string categoryName)
    {
        return new ProductResponse(
            product.Id,
            product.CategoryId,
            categoryName,
            product.Sku,
            product.Barcode,
            product.Name,
            product.Description,
            product.Price,
            product.TaxRate,
            product.Cost,
            product.IsActive,
            product.TrackStock,
            product.Stock);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed record CategoryUpsertRequest(
    string Name,
    string? Description,
    bool IsActive = true);

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);

public sealed record ProductUpsertRequest(
    Guid CategoryId,
    string Sku,
    string? Barcode,
    string Name,
    string? Description,
    decimal Price,
    decimal TaxRate,
    decimal? Cost,
    bool IsActive = true,
    bool TrackStock = false,
    int Stock = 0);

public sealed record ProductResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Sku,
    string? Barcode,
    string Name,
    string? Description,
    decimal Price,
    decimal TaxRate,
    decimal? Cost,
    bool IsActive,
    bool TrackStock,
    int Stock);
