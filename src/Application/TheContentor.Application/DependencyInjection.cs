using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace TheContentor.Application;

/// <summary>Service registration entry point for the application layer.</summary>
public static class DependencyInjection
{
    /// <summary>Adds application services to the DI container.</summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
