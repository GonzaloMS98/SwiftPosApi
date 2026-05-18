using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftPos.Api.Security;
using SwiftPos.Domain.Users;
using SwiftPos.Infrastructure.Persistence;

namespace SwiftPos.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleCodes.Owner + "," + RoleCodes.Admin)]
[Route("users")]
public sealed class UsersController(
    SwiftPosDbContext dbContext,
    CurrentUserContext currentUserContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> List(CancellationToken cancellationToken)
    {
        var currentUser = currentUserContext.GetRequired();
        var users = await (
            from user in dbContext.Users.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on user.RoleId equals role.Id
            join storeCandidate in dbContext.Stores.AsNoTracking() on user.StoreId equals storeCandidate.Id into stores
            from store in stores.DefaultIfEmpty()
            where user.TenantId == currentUser.TenantId
            orderby user.FullName
            select new UserResponse(
                user.Id,
                user.FullName,
                user.Email,
                role.Code,
                user.StoreId,
                store == null ? null : store.Name,
                user.IsActive,
                user.LastLoginAt))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }
}

public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    Guid? StoreId,
    string? StoreName,
    bool IsActive,
    DateTimeOffset? LastLoginAt);
