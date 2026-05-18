using SwiftPos.Domain.Common;

namespace SwiftPos.Domain.Pos;

public sealed class SaleItem : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string ProductSkuSnapshot { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
}
