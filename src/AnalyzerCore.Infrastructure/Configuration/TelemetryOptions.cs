using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Configuration;

/// <summary>
/// Configuration options for telemetry (metrics and tracing).
/// </summary>
public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    /// <summary>
    /// Whether telemetry is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The service name for telemetry identification.
    /// </summary>
    [Required]
    public string ServiceName { get; set; } = "AnalyzerCore";

    /// <summary>
    /// The service version.
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Whether to expose Prometheus metrics endpoint.
    /// </summary>
    public bool PrometheusEnabled { get; set; } = true;

    /// <summary>
    /// The path for Prometheus metrics endpoint.
    /// </summary>
    public string PrometheusEndpoint { get; set; } = "/metrics";

    /// <summary>
    /// Whether to enable HTTP client instrumentation.
    /// </summary>
    public bool HttpClientInstrumentation { get; set; } = true;

    /// <summary>
    /// Whether to enable ASP.NET Core instrumentation.
    /// </summary>
    public bool AspNetCoreInstrumentation { get; set; } = true;

    // ============ Tracing Configuration ============

    /// <summary>
    /// Whether distributed tracing is enabled.
    /// </summary>
    public bool TracingEnabled { get; set; } = true;

    /// <summary>
    /// Whether to export traces to Jaeger.
    /// </summary>
    public bool JaegerEnabled { get; set; } = true;

    /// <summary>
    /// Jaeger agent host for UDP transport.
    /// </summary>
    public string JaegerAgentHost { get; set; } = "localhost";

    /// <summary>
    /// Jaeger agent port for UDP transport.
    /// </summary>
    public int JaegerAgentPort { get; set; } = 6831;

    /// <summary>
    /// Sampling ratio for traces (0.0 to 1.0).
    /// 1.0 = sample all traces, 0.1 = sample 10% of traces.
    /// </summary>
    public double TracingSamplingRatio { get; set; } = 1.0;

    /// <summary>
    /// Whether to enable Entity Framework Core instrumentation for database tracing.
    /// </summary>
    public bool EntityFrameworkInstrumentation { get; set; } = true;

    /// <summary>
    /// Whether to set the DB statement as span name for EF Core instrumentation.
    /// </summary>
    public bool EfCoreSetDbStatementForText { get; set; } = false;

    /// <summary>
    /// Whether to enrich spans with exception details.
    /// </summary>
    public bool EnrichWithException { get; set; } = true;

    /// <summary>
    /// Whether to record exception stack traces in spans.
    /// </summary>
    public bool RecordException { get; set; } = true;
}
