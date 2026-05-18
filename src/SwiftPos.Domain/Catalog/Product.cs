using SwiftPos.Domain.Common;

namespace SwiftPos.Domain.Catalog;

public sealed class Product : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid CategoryId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal TaxRate { get; set; }
    public decimal? Cost { get; set; }
    public bool IsActive { get; set; } = true;
    public bool TrackStock { get; set; }
    public int Stock { get; set; }
}
