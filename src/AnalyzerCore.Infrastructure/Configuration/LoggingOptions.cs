using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Configuration;

/// <summary>
/// Configuration options for structured logging with Serilog and Seq.
/// </summary>
public sealed class LoggingOptions
{
    public const string SectionName = "Logging";

    /// <summary>
    /// Whether Seq integration is enabled.
    /// </summary>
    public bool SeqEnabled { get; set; } = false;

    /// <summary>
    /// Seq server URL.
    /// </summary>
    public string SeqServerUrl { get; set; } = "http://localhost:5341";

    /// <summary>
    /// Seq API key for authentication (optional).
    /// </summary>
    public string? SeqApiKey { get; set; }

    /// <summary>
    /// Default minimum log level.
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Whether to enrich logs with machine name.
    /// </summary>
    public bool EnrichWithMachineName { get; set; } = true;

    /// <summary>
    /// Whether to enrich logs with environment name.
    /// </summary>
    public bool EnrichWithEnvironmentName { get; set; } = true;

    /// <summary>
    /// Whether to enrich logs with process ID.
    /// </summary>
    public bool EnrichWithProcessId { get; set; } = true;

    /// <summary>
    /// Whether to enrich logs with thread ID.
    /// </summary>
    public bool EnrichWithThreadId { get; set; } = true;

    /// <summary>
    /// Whether to enrich logs with exception details.
    /// </summary>
    public bool EnrichWithExceptionDetails { get; set; } = true;

    /// <summary>
    /// Application name for log context.
    /// </summary>
    [Required]
    public string ApplicationName { get; set; } = "AnalyzerCore";

    /// <summary>
    /// Application version for log context.
    /// </summary>
    public string ApplicationVersion { get; set; } = "1.0.0";

    /// <summary>
    /// List of properties to mask in logs (e.g., Password, ApiKey).
    /// </summary>
    public List<string> SensitiveProperties { get; set; } = new()
    {
        "Password",
        "Secret",
        "Token",
        "ApiKey",
        "Authorization",
        "Credential",
        "PrivateKey",
        "ConnectionString"
    };

    /// <summary>
    /// Log level overrides per namespace.
    /// </summary>
    public Dictionary<string, string> LevelOverrides { get; set; } = new()
    {
        { "Microsoft", "Warning" },
        { "Microsoft.EntityFrameworkCore", "Warning" },
        { "Microsoft.AspNetCore", "Warning" },
        { "System.Net.Http", "Warning" }
    };
}
