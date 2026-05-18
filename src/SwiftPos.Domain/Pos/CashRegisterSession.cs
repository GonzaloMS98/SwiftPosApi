using SwiftPos.Domain.Common;

namespace SwiftPos.Domain.Pos;

public sealed class CashRegisterSession : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid StoreId { get; set; }
    public Guid OpenedByUserId { get; set; }
    public Guid? ClosedByUserId { get; set; }
    public string Status { get; set; } = CashRegisterSessionStatuses.Open;
    public decimal OpeningCashAmount { get; set; }
    public decimal? ClosingCashAmount { get; set; }
    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }
}
