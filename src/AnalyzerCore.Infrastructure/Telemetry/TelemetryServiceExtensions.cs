using AnalyzerCore.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
            })
            .WithTracing(tracing =>
            {
                if (!options.TracingEnabled)
                {
                    return;
                }

                // Configure sampling
                tracing.SetSampler(new TraceIdRatioBasedSampler(options.TracingSamplingRatio));

                // Add ASP.NET Core instrumentation
                if (options.AspNetCoreInstrumentation)
                {
                    tracing.AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.RecordException = options.RecordException;
                        opts.EnrichWithException = options.EnrichWithException
                            ? (activity, exception) =>
                            {
                                activity?.SetTag("exception.type", exception.GetType().FullName);
                                activity?.SetTag("exception.message", exception.Message);
                            }
                            : null;
                    });
                }

                // Add HTTP client instrumentation
                if (options.HttpClientInstrumentation)
                {
                    tracing.AddHttpClientInstrumentation(opts =>
                    {
                        opts.RecordException = options.RecordException;
                    });
                }

                // Add Entity Framework Core instrumentation
                if (options.EntityFrameworkInstrumentation)
                {
                    tracing.AddEntityFrameworkCoreInstrumentation(opts =>
                    {
                        opts.SetDbStatementForText = options.EfCoreSetDbStatementForText;
                    });
                }

                // Add custom activity sources
                tracing.AddSource(ApplicationMetrics.MeterName);
                foreach (var sourceName in ActivitySources.AllSourceNames)
                {
                    tracing.AddSource(sourceName);
                }

                // Export to Jaeger via OTLP (Jaeger supports OTLP natively)
                if (options.JaegerEnabled)
                {
                    tracing.AddOtlpExporter(otlp =>
                    {
                        // Jaeger OTLP endpoint (default: http://localhost:4317)
                        otlp.Endpoint = new Uri($"http://{options.JaegerAgentHost}:4317");
                    });
                }

                // Also export to console in development for debugging
                #if DEBUG
                tracing.AddConsoleExporter();
                #endif
            });

        return services;
    }
}
