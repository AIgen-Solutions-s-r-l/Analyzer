using MediatR;

namespace AnalyzerCore.Domain.Abstractions;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain that domain experts care about.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// The unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// The UTC timestamp when this event occurred.
    /// </summary>
    DateTime OccurredOnUtc { get; }
}

/// <summary>
/// Base record for domain events providing common properties.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
