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
}
