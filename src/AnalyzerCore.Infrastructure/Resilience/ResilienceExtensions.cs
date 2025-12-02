using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnalyzerCore.Infrastructure.Resilience;

/// <summary>
/// Extension methods for resilience policy registration.
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    /// Adds resilience policies to the service collection.
    /// </summary>
    public static IServiceCollection AddResiliencePolicies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure options
        services.AddOptions<ResilienceOptions>()
            .Bind(configuration.GetSection(ResilienceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register policy factory
        services.AddSingleton<IResiliencePolicyFactory, ResiliencePolicyFactory>();

        // Register resilient HTTP handler
        services.AddTransient<ResilientHttpClientHandler>();

        return services;
    }

    /// <summary>
    /// Adds a named HTTP client with resilience policies.
    /// </summary>
    public static IHttpClientBuilder AddResilientHttpClient(
        this IServiceCollection services,
        string name,
        Action<HttpClient>? configureClient = null)
    {
        var builder = services.AddHttpClient(name, client =>
        {
            configureClient?.Invoke(client);
        });

        builder.AddHttpMessageHandler<ResilientHttpClientHandler>();

        return builder;
    }
}
