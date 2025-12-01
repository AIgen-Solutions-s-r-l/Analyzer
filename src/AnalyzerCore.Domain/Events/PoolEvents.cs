using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Domain.Events;

/// <summary>
/// Raised when a new liquidity pool is discovered and created.
/// </summary>
public sealed record PoolCreatedDomainEvent(
    string PoolAddress,
    string Token0Address,
    string Token1Address,
    string FactoryAddress,
    PoolType PoolType) : DomainEvent;

/// <summary>
/// Raised when pool reserves are updated.
/// </summary>
public sealed record PoolReservesUpdatedDomainEvent(
    string PoolAddress,
    decimal PreviousReserve0,
    decimal PreviousReserve1,
    decimal NewReserve0,
    decimal NewReserve1) : DomainEvent;
