using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftPos.Api.Security;
using SwiftPos.Infrastructure.Persistence;

namespace SwiftPos.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(
    SwiftPosDbContext dbContext,
    PasswordHasher passwordHasher,
    JwtTokenService jwtTokenService,
    CurrentUserContext currentUserContext) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized();
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .AsTracking()
            .FirstOrDefaultAsync(candidate => candidate.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized();
        }

        var role = await dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == user.RoleId && candidate.TenantId == user.TenantId, cancellationToken);

        if (role is null)
        {
            return Unauthorized();
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var authenticatedUser = new AuthenticatedUser(user.Id, user.TenantId, user.StoreId, role.Code);
        var token = jwtTokenService.CreateToken(authenticatedUser);

        return Ok(new LoginResponse(
            token.AccessToken,
            "Bearer",
            token.ExpiresAtUtc,
            new AuthUserResponse(user.Id, user.FullName, user.Email, user.TenantId, user.StoreId, role.Code)));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        var currentUser = currentUserContext.GetRequired();
        var profile = await (
            from user in dbContext.Users.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on user.RoleId equals role.Id
            join tenant in dbContext.Tenants.AsNoTracking() on user.TenantId equals tenant.Id
            join storeCandidate in dbContext.Stores.AsNoTracking() on user.StoreId equals storeCandidate.Id into stores
            from store in stores.DefaultIfEmpty()
            where user.Id == currentUser.UserId && user.TenantId == currentUser.TenantId
            select new MeResponse(
                new AuthUserResponse(user.Id, user.FullName, user.Email, user.TenantId, user.StoreId, role.Code),
                new CurrentTenantResponse(tenant.Id, tenant.Name, tenant.Slug, tenant.Status),
                store == null ? null : new CurrentStoreResponse(store.Id, store.Name, store.Code, store.IsActive)))
            .FirstOrDefaultAsync(cancellationToken);

        return profile is null ? Unauthorized() : Ok(profile);
    }
}

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    AuthUserResponse User);

public sealed record MeResponse(
    AuthUserResponse User,
    CurrentTenantResponse Tenant,
    CurrentStoreResponse? Store);

public sealed record AuthUserResponse(
    Guid Id,
    string FullName,
    string Email,
    Guid TenantId,
    Guid? StoreId,
    string Role);

public sealed record CurrentTenantResponse(
    Guid Id,
    string Name,
    string Slug,
    string Status);

public sealed record CurrentStoreResponse(
    Guid Id,
    string Name,
    string Code,
    bool IsActive);
