using Npgsql;
using SwiftPos.Infrastructure;

namespace SwiftPos.Tests;

public sealed class PostgresConnectionStringTests
{
    [Fact]
    public void NormalizePostgresConnectionString_ConvertsUriFormatForNpgsql()
    {
        var normalized = DependencyInjection.NormalizePostgresConnectionString(
            "postgresql://demo_user:p%40ssword@example.neon.tech:5433/demo_db?sslmode=require");

        var builder = new NpgsqlConnectionStringBuilder(normalized);

        Assert.Equal("example.neon.tech", builder.Host);
        Assert.Equal(5433, builder.Port);
        Assert.Equal("demo_db", builder.Database);
        Assert.Equal("demo_user", builder.Username);
        Assert.Equal("p@ssword", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }
}
