using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheContentor.Infrastructure.Interfaces;
using TheContentor.Infrastructure.Services;

namespace TheContentor.Infrastructure;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddInfrastructureServices(this IHostApplicationBuilder builder, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("the-contentor-db");

        builder.Services.AddDbContext<TheContentorDbContext>(options =>
            options.UseNpgsql(connectionString));

        builder.AddAzureBlobServiceClient("blobs");

        builder.Services.AddScoped<IBlobService, BlobService>();

        return builder;
    }

    public static async Task ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TheContentorDbContext>();
        await context.Database.MigrateAsync();
    }
}
