using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftPos.Api.Security;
using SwiftPos.Infrastructure.Persistence;

namespace SwiftPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("stores")]
public sealed class StoresController(
    SwiftPosDbContext dbContext,
    CurrentUserContext currentUserContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StoreResponse>>> List(CancellationToken cancellationToken)
    {
        var currentUser = currentUserContext.GetRequired();
        var stores = await dbContext.Stores
            .AsNoTracking()
            .Where(store => store.TenantId == currentUser.TenantId)
            .OrderBy(store => store.Name)
            .Select(store => new StoreResponse(store.Id, store.Name, store.Code, store.Address, store.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(stores);
    }
}

public sealed record StoreResponse(
    Guid Id,
    string Name,
    string Code,
    string? Address,
    bool IsActive);
