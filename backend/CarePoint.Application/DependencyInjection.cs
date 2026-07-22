using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CarePoint.Application;

/// <summary>
/// Registers Application layer services: validators.
/// Service implementations are registered in Infrastructure since they depend on DbContext/Identity.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
