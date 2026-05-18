using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SwiftPos.Api.Security;

namespace SwiftPos.Tests;

public sealed class CurrentUserContextTests
{
    [Fact]
    public void GetRequired_ReadsAuthenticatedClaims()
    {
        var userId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var storeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(AuthClaimTypes.UserId, userId.ToString()),
                    new Claim(AuthClaimTypes.TenantId, tenantId.ToString()),
                    new Claim(AuthClaimTypes.StoreId, storeId.ToString()),
                    new Claim(AuthClaimTypes.Role, "admin")
                ], "Bearer"))
            }
        };

        var currentUser = new CurrentUserContext(httpContextAccessor).GetRequired();

        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal(tenantId, currentUser.TenantId);
        Assert.Equal(storeId, currentUser.StoreId);
        Assert.Equal("admin", currentUser.Role);
    }
}
