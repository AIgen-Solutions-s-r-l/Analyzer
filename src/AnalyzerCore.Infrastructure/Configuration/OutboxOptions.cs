using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the outbox processor.
/// </summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// Interval in seconds between outbox processing runs.
    /// </summary>
    [Range(1, 300)]
    public int ProcessingIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Number of messages to process in each batch.
    /// </summary>
    [Range(1, 100)]
    public int BatchSize { get; set; } = 20;

    /// <summary>
    /// Maximum number of retries for failed messages.
    /// </summary>
    [Range(1, 10)]
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Whether the outbox processor is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
