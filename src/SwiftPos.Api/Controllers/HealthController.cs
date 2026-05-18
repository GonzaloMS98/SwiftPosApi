using Microsoft.AspNetCore.Mvc;

namespace SwiftPos.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse(
            Service: "SwiftPosApi",
            Status: "ok",
            TimestampUtc: DateTimeOffset.UtcNow));
    }
}

public sealed record HealthResponse(
    string Service,
    string Status,
    DateTimeOffset TimestampUtc);
