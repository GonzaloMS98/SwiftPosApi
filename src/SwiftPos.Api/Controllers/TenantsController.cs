using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftPos.Api.Security;
using SwiftPos.Infrastructure.Persistence;

namespace SwiftPos.Api.Controllers;

[ApiController]
[Authorize]
[Route("tenants")]
public sealed class TenantsController(
    SwiftPosDbContext dbContext,
    CurrentUserContext currentUserContext) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<CurrentTenantResponse>> Current(CancellationToken cancellationToken)
    {
        var currentUser = currentUserContext.GetRequired();
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .Where(candidate => candidate.Id == currentUser.TenantId)
            .Select(candidate => new CurrentTenantResponse(candidate.Id, candidate.Name, candidate.Slug, candidate.Status))
            .FirstOrDefaultAsync(cancellationToken);

        return tenant is null ? NotFound() : Ok(tenant);
    }
}
