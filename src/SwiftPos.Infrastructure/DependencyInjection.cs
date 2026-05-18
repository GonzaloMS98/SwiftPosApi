using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddDbContext<SwiftPosDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
