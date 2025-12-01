using System;

namespace AnalyzerCore.Domain.Entities;

/// <summary>
/// Represents an outbox message for reliable domain event publishing.
/// Part of the Transactional Outbox Pattern.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string Content { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    private OutboxMessage()
    {
        // Required by EF Core
        Type = string.Empty;
        Content = string.Empty;
    }

    public OutboxMessage(Guid id, string type, string content, DateTime occurredOnUtc)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
        ProcessedOnUtc = null;
        Error = null;
        RetryCount = 0;
    }

    public void MarkAsProcessed(DateTime processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}
