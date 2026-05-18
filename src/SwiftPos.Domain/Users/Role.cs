using SwiftPos.Domain.Common;

namespace SwiftPos.Domain.Users;

public sealed class Role : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = RoleCodes.Cashier;
}
