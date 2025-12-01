namespace AnalyzerCore.Domain.Abstractions;

/// <summary>
/// Marker interface for domain entities.
/// </summary>
public interface IEntity
{
}

/// <summary>
/// Marker interface for domain entities with a specific identifier type.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public interface IEntity<TId> : IEntity where TId : notnull
{
    TId Id { get; }
}
