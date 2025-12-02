using AnalyzerCore.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace AnalyzerCore.Infrastructure.Telemetry;

/// <summary>
/// Extension methods for configuring OpenTelemetry and metrics.
/// </summary>
public static class TelemetryServiceExtensions
{
    /// <summary>
    /// Adds OpenTelemetry metrics and tracing to the service collection.
    /// </summary>
    public static IServiceCollection AddTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>()
            ?? new TelemetryOptions();

        if (!options.Enabled)
        {
            return services;
        }

        // Configure Options
        services
            .AddOptions<TelemetryOptions>()
            .Bind(configuration.GetSection(TelemetryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register ApplicationMetrics as singleton
        services.AddSingleton<ApplicationMetrics>();

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: options.ServiceName,
                    serviceVersion: options.ServiceVersion))
            .WithMetrics(metrics =>
            {
                // Add application-specific metrics
                metrics.AddMeter(ApplicationMetrics.MeterName);

                // Add ASP.NET Core instrumentation
                if (options.AspNetCoreInstrumentation)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }

                // Add HTTP client instrumentation
                if (options.HttpClientInstrumentation)
                {
                    metrics.AddHttpClientInstrumentation();
                }

                // Add runtime metrics
                metrics.AddRuntimeInstrumentation();

                // Export to Prometheus
                if (options.PrometheusEnabled)
                {
                    metrics.AddPrometheusExporter();
                }
            });

        return services;
    }
}
