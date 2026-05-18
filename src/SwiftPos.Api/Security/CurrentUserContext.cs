using System.Security.Claims;

namespace SwiftPos.Api.Security;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor)
{
    public AuthenticatedUser GetRequired()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("The request is not authenticated.");
        }

        var userId = ReadRequiredGuid(user, AuthClaimTypes.UserId);
        var tenantId = ReadRequiredGuid(user, AuthClaimTypes.TenantId);
        var storeId = ReadOptionalGuid(user, AuthClaimTypes.StoreId);
        var role = user.FindFirstValue(AuthClaimTypes.Role)
            ?? user.FindFirstValue(ClaimTypes.Role)
            ?? throw new UnauthorizedAccessException("The authenticated user has no role claim.");

        return new AuthenticatedUser(userId, tenantId, storeId, role);
    }

    private static Guid ReadRequiredGuid(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);
        if (!Guid.TryParse(value, out var parsed))
        {
            throw new UnauthorizedAccessException($"The authenticated user has no valid {claimType} claim.");
        }

        return parsed;
    }

    private static Guid? ReadOptionalGuid(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Guid.TryParse(value, out var parsed))
        {
            throw new UnauthorizedAccessException($"The authenticated user has no valid {claimType} claim.");
        }

        return parsed;
    }
}
