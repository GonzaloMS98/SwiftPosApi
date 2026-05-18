using SwiftPos.Domain.Common;

namespace SwiftPos.Domain.Tenants;

public sealed class Tenant : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = TenantStatuses.Active;
}
