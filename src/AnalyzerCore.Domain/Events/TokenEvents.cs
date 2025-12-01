using AnalyzerCore.Domain.Abstractions;

namespace AnalyzerCore.Domain.Events;

/// <summary>
/// Raised when a new token is discovered and created.
/// </summary>
public sealed record TokenCreatedDomainEvent(
    string TokenAddress,
    string Symbol,
    string Name,
    int Decimals,
    string ChainId) : DomainEvent;

/// <summary>
/// Raised when a placeholder token's information is updated with real data.
/// </summary>
public sealed record TokenInfoUpdatedDomainEvent(
    string TokenAddress,
    string OldSymbol,
    string NewSymbol,
    string OldName,
    string NewName) : DomainEvent;
