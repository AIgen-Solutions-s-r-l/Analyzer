using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the cleanup background job.
/// </summary>
public sealed class CleanupOptions
{
    public const string SectionName = "Cleanup";

    /// <summary>
    /// Whether the cleanup job is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval between cleanup runs in minutes.
    /// </summary>
    [Range(1, 1440)]
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Number of days to retain processed outbox messages.
    /// </summary>
    [Range(1, 365)]
    public int OutboxRetentionDays { get; set; } = 7;

    /// <summary>
    /// Number of days to retain idempotent request records.
    /// </summary>
    [Range(1, 365)]
    public int IdempotencyRetentionDays { get; set; } = 7;

    /// <summary>
    /// Maximum number of records to delete per batch.
    /// </summary>
    [Range(100, 10000)]
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Whether to also delete failed outbox messages after retention period.
    /// </summary>
    public bool DeleteFailedMessages { get; set; } = true;
}
