using SwiftPos.Domain.Common;

namespace SwiftPos.Domain.Users;

public sealed class User : Entity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid? StoreId { get; set; }
    public Guid RoleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }
}
