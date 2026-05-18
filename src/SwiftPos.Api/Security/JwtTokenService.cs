using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SwiftPos.Api.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public AuthToken CreateToken(AuthenticatedUser user)
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        var expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(AuthClaimTypes.UserId, user.UserId.ToString()),
            new(AuthClaimTypes.TenantId, user.TenantId.ToString()),
            new(AuthClaimTypes.Role, user.Role),
            new(ClaimTypes.Role, user.Role)
        };

        if (user.StoreId is not null)
        {
            claims.Add(new Claim(AuthClaimTypes.StoreId, user.StoreId.Value.ToString()));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return new AuthToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}

public sealed record AuthenticatedUser(
    Guid UserId,
    Guid TenantId,
    Guid? StoreId,
    string Role);

public sealed record AuthToken(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);
