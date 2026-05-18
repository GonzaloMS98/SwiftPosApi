using SwiftPos.Domain.Common;

namespace SwiftPos.Domain.Pos;

public sealed class Order : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid StoreId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? CashRegisterSessionId { get; set; }
    public string Mode { get; set; } = OrderModes.Takeaway;
    public string Status { get; set; } = OrderStatuses.Created;
    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
}
