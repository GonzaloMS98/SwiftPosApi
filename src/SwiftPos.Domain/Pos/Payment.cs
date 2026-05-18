using SwiftPos.Domain.Common;

namespace SwiftPos.Domain.Pos;

public sealed class Payment : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid StoreId { get; set; }
    public Guid SaleId { get; set; }
    public string Method { get; set; } = PaymentMethods.Cash;
    public string Status { get; set; } = PaymentStatuses.Completed;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public DateTimeOffset PaidAt { get; set; } = DateTimeOffset.UtcNow;
}
