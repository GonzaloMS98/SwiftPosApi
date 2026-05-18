namespace SwiftPos.Domain.Common;

public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
