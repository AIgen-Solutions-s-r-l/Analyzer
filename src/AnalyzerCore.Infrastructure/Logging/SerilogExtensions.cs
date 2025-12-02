using AnalyzerCore.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

namespace AnalyzerCore.Infrastructure.Logging;

/// <summary>
/// Extension methods for configuring Serilog with Seq integration.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog with structured logging and optional Seq integration.
    /// </summary>
    public static IHostBuilder UseSerilogLogging(
        this IHostBuilder hostBuilder,
        IConfiguration configuration)
    {
        var loggingOptions = configuration.GetSection(LoggingOptions.SectionName).Get<LoggingOptions>()
            ?? new LoggingOptions();

        return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            ConfigureLogging(loggerConfiguration, loggingOptions, context.HostingEnvironment.EnvironmentName);
        });
    }

    /// <summary>
    /// Creates a bootstrap logger for startup logging.
    /// </summary>
    public static LoggerConfiguration CreateBootstrapLogger(IConfiguration configuration)
    {
        var loggingOptions = configuration.GetSection(LoggingOptions.SectionName).Get<LoggingOptions>()
            ?? new LoggingOptions();

        var loggerConfig = new LoggerConfiguration();
        ConfigureLogging(loggerConfig, loggingOptions, "Bootstrap");

        return loggerConfig;
    }

    /// <summary>
    /// Adds Serilog services to the DI container.
    /// </summary>
    public static IServiceCollection AddSerilogServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LoggingOptions>(configuration.GetSection(LoggingOptions.SectionName));

        // Register request context enricher
        services.AddHttpContextAccessor();
        services.AddSingleton<RequestContextEnricher>();

        return services;
    }

    /// <summary>
    /// Adds Serilog request logging middleware.
    /// </summary>
    public static IApplicationBuilder UseSerilogRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
                diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());

                if (httpContext.User?.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserId",
                        httpContext.User.FindFirst("sub")?.Value
                        ?? httpContext.User.Identity.Name);
                }

                var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                                  ?? httpContext.Items["CorrelationId"]?.ToString();
                if (!string.IsNullOrEmpty(correlationId))
                {
                    diagnosticContext.Set("CorrelationId", correlationId);
                }
            };

            // Customize the log level based on status code
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 500)
                    return LogEventLevel.Error;

                if (httpContext.Response.StatusCode >= 400)
                    return LogEventLevel.Warning;

                // Health check endpoints should be logged as Debug
                if (httpContext.Request.Path.StartsWithSegments("/health"))
                    return LogEventLevel.Debug;

                // Metrics endpoint should be logged as Debug
                if (httpContext.Request.Path.StartsWithSegments("/metrics"))
                    return LogEventLevel.Debug;

                return LogEventLevel.Information;
            };
        });
    }

    private static void ConfigureLogging(
        LoggerConfiguration loggerConfiguration,
        LoggingOptions options,
        string environmentName)
    {
        // Parse minimum level
        var minimumLevel = Enum.TryParse<LogEventLevel>(options.MinimumLevel, true, out var level)
            ? level
            : LogEventLevel.Information;

        loggerConfiguration
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", options.ApplicationName)
            .Enrich.WithProperty("Version", options.ApplicationVersion)
            .Enrich.WithProperty("Environment", environmentName);

        // Apply level overrides
        foreach (var (source, levelOverride) in options.LevelOverrides)
        {
            if (Enum.TryParse<LogEventLevel>(levelOverride, true, out var overrideLevel))
            {
                loggerConfiguration.MinimumLevel.Override(source, overrideLevel);
            }
        }

        // Enrich with machine name
        if (options.EnrichWithMachineName)
        {
            loggerConfiguration.Enrich.WithMachineName();
        }

        // Enrich with environment
        if (options.EnrichWithEnvironmentName)
        {
            loggerConfiguration.Enrich.WithEnvironmentName();
        }

        // Enrich with process ID
        if (options.EnrichWithProcessId)
        {
            loggerConfiguration.Enrich.WithProcessId();
        }

        // Enrich with thread ID
        if (options.EnrichWithThreadId)
        {
            loggerConfiguration.Enrich.WithThreadId();
        }

        // Enrich with exception details
        if (options.EnrichWithExceptionDetails)
        {
            loggerConfiguration.Enrich.WithExceptionDetails();
        }

        // Add sensitive data masking
        if (options.SensitiveProperties.Any())
        {
            loggerConfiguration.Destructure.With(
                new SensitiveDataDestructuringPolicy(options.SensitiveProperties));
        }

        // Console sink with structured output
        loggerConfiguration.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        // Seq sink if enabled
        if (options.SeqEnabled && !string.IsNullOrEmpty(options.SeqServerUrl))
        {
            loggerConfiguration.WriteTo.Seq(
                serverUrl: options.SeqServerUrl,
                apiKey: options.SeqApiKey,
                restrictedToMinimumLevel: minimumLevel);
        }

        // File sink for production (JSON format for log aggregation)
        loggerConfiguration.WriteTo.File(
            formatter: new CompactJsonFormatter(),
            path: "logs/analyzercore-.json",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            fileSizeLimitBytes: 100_000_000); // 100 MB
    }
}
