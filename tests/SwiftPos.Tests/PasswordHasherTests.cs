using SwiftPos.Api.Security;

namespace SwiftPos.Tests;

public sealed class PasswordHasherTests
{
    private const string DemoPasswordHash =
        "pbkdf2-sha256:100000:U3dpZnRQT1MgZGVtbyBzYWx0IHYx:vY8JJtZHeQz+GKC0pyPN71oEoiAeyXk3MXmEi3VxptM=";

    [Fact]
    public void Verify_AcceptsSeededDevelopmentPassword()
    {
        var hasher = new PasswordHasher();

        Assert.True(hasher.Verify("SwiftposDemo123!", DemoPasswordHash));
    }

    [Fact]
    public void Verify_RejectsInvalidPassword()
    {
        var hasher = new PasswordHasher();

        Assert.False(hasher.Verify("wrong-password", DemoPasswordHash));
    }
}
