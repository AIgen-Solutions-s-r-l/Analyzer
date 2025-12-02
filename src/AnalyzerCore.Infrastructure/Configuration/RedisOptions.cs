using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Redis cache.
/// </summary>
public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// Whether Redis caching is enabled. If false, falls back to in-memory cache.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Redis connection string.
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Instance name for key prefixing.
    /// </summary>
    public string InstanceName { get; set; } = "AnalyzerCore:";

    /// <summary>
    /// Default expiration time in seconds for cache entries.
    /// </summary>
    [Range(1, 86400)]
    public int DefaultExpirationSeconds { get; set; } = 300;

    /// <summary>
    /// Connection timeout in milliseconds.
    /// </summary>
    [Range(1000, 60000)]
    public int ConnectTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Sync timeout in milliseconds.
    /// </summary>
    [Range(1000, 60000)]
    public int SyncTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Whether to abort on connection failure (false = reconnect automatically).
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = false;

    /// <summary>
    /// SSL/TLS connection.
    /// </summary>
    public bool Ssl { get; set; } = false;

    /// <summary>
    /// Password for Redis authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Number of retry attempts for failed operations.
    /// </summary>
    [Range(0, 10)]
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Base delay between retries in milliseconds.
    /// </summary>
    [Range(100, 5000)]
    public int RetryBaseDelayMs { get; set; } = 500;

    /// <summary>
    /// Gets the full connection string with options.
    /// </summary>
    public string GetFullConnectionString()
    {
        var options = new List<string> { ConnectionString };

        if (!string.IsNullOrEmpty(Password))
        {
            options.Add($"password={Password}");
        }

        options.Add($"connectTimeout={ConnectTimeoutMs}");
        options.Add($"syncTimeout={SyncTimeoutMs}");
        options.Add($"abortConnect={AbortOnConnectFail.ToString().ToLower()}");

        if (Ssl)
        {
            options.Add("ssl=true");
        }

        return string.Join(",", options);
    }
}
