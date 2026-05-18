using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SwiftPos.Infrastructure.Persistence;

namespace SwiftPos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=swiftpos;Username=swiftpos;Password=swiftpos_dev";
        }

        connectionString = NormalizePostgresConnectionString(connectionString);

        services.AddDbContext<SwiftPosDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static string NormalizePostgresConnectionString(string connectionString)
    {
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgresql" && uri.Scheme != "postgres"))
        {
            return connectionString;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty),
            Password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty)
        };

        if (!uri.IsDefaultPort)
        {
            builder.Port = uri.Port;
        }

        foreach (var queryPart in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var keyValue = queryPart.Split('=', 2);
            var key = Uri.UnescapeDataString(keyValue[0]);
            var value = Uri.UnescapeDataString(keyValue.ElementAtOrDefault(1) ?? string.Empty);

            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                builder["SSL Mode"] = value;
            }
        }

        return builder.ConnectionString;
    }
}
