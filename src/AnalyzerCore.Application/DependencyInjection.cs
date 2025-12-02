using AnalyzerCore.Application.Behaviors;
using AnalyzerCore.Application.Pools.Commands.CreatePool;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AnalyzerCore.Application;

/// <summary>
/// Extension methods for registering Application layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Application layer services to the DI container.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Register MediatR handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register pipeline behaviors in order of execution
        // 1. Correlation ID (first, to ensure all logs have correlation ID)
        // 2. Logging (to log all requests)
        // 3. Idempotency (early, to prevent duplicate processing)
        // 4. Validation (before processing)
        // 5. Unit of Work (commits after successful handling)
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CorrelationIdBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        return services;
    }
}
