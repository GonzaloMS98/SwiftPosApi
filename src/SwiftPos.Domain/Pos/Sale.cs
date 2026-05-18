using SwiftPos.Domain.Common;

namespace SwiftPos.Domain.Pos;

public sealed class Sale : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid StoreId { get; set; }
    public Guid OrderId { get; set; }
    public Guid SoldByUserId { get; set; }
    public Guid? CashRegisterSessionId { get; set; }
    public string Status { get; set; } = SaleStatuses.Completed;
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
}
