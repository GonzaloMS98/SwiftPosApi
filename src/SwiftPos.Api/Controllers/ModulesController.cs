using Microsoft.AspNetCore.Mvc;

namespace SwiftPos.Api.Controllers;

[ApiController]
[Route("modules")]
public sealed class ModulesController : ControllerBase
{
    private static readonly ModuleStatus[] Modules =
    [
        new("auth", "implemented", 0),
        new("tenants", "implemented", 0),
        new("stores", "implemented", 0),
        new("users", "implemented", 0),
        new("catalog", "partial", 1),
        new("pos", "partial", 1),
        new("orders", "partial", 1),
        new("payments", "partial", 1),
        new("stats", "planned", 1)
    ];

    [HttpGet]
    public ActionResult<IReadOnlyCollection<ModuleStatus>> Get()
    {
        return Ok(Modules);
    }
}

public sealed record ModuleStatus(
    string Code,
    string Status,
    int Sprint);
