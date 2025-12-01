using System;

namespace AnalyzerCore.Domain.Entities;

/// <summary>
/// Represents an idempotent request to prevent duplicate command processing.
/// </summary>
public sealed class IdempotentRequest
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private IdempotentRequest()
    {
        // Required by EF Core
        Name = string.Empty;
    }

    public IdempotentRequest(Guid id, string name, DateTime createdOnUtc)
    {
        Id = id;
        Name = name;
        CreatedOnUtc = createdOnUtc;
    }
}
