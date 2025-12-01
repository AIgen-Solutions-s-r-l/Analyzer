using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Configuration;

/// <summary>
/// Configuration options for database connection.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    /// <summary>
    /// The default database connection string.
    /// </summary>
    [Required]
    public string DefaultConnection { get; set; } = "Data Source=local.db";

    /// <summary>
    /// Whether to enable sensitive data logging (for development only).
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Command timeout in seconds.
    /// </summary>
    [Range(1, 300)]
    public int CommandTimeout { get; set; } = 30;
}
