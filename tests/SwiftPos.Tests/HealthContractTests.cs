using SwiftPos.Api.Controllers;

namespace SwiftPos.Tests;

public sealed class HealthContractTests
{
    [Fact]
    public void HealthResponse_UsesExpectedServiceAndStatus()
    {
        var response = new HealthResponse("SwiftPosApi", "ok", DateTimeOffset.UtcNow);

        Assert.Equal("SwiftPosApi", response.Service);
        Assert.Equal("ok", response.Status);
    }
}
