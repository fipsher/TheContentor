using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TheContentor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("the-contentor-db");

        services.AddDbContext<TheContentorDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static async Task ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TheContentorDbContext>();
        await context.Database.MigrateAsync();
    }
}
