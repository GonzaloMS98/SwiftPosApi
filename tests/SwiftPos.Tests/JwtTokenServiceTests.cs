using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using SwiftPos.Api.Security;

namespace SwiftPos.Tests;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_IncludesTenantStoreUserAndRoleClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "SwiftPOS.Tests",
            Audience = "SwiftPOS.Tests.Clients",
            SigningKey = "swiftpos-tests-signing-key-with-enough-length-32",
            ExpirationMinutes = 30
        });
        var user = new AuthenticatedUser(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "admin");

        var token = new JwtTokenService(options).CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

        Assert.Equal("SwiftPOS.Tests", jwt.Issuer);
        Assert.Contains(jwt.Audiences, audience => audience == "SwiftPOS.Tests.Clients");
        Assert.Contains(jwt.Claims, claim => claim.Type == AuthClaimTypes.UserId && claim.Value == user.UserId.ToString());
        Assert.Contains(jwt.Claims, claim => claim.Type == AuthClaimTypes.TenantId && claim.Value == user.TenantId.ToString());
        Assert.Contains(jwt.Claims, claim => claim.Type == AuthClaimTypes.StoreId && claim.Value == user.StoreId.ToString());
        Assert.Contains(jwt.Claims, claim => claim.Type == AuthClaimTypes.Role && claim.Value == "admin");
        Assert.True(token.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }
}
