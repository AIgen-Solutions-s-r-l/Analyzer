using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Liquidity.Queries.GetPoolMetrics;

/// <summary>
/// Query to get liquidity metrics for a specific pool.
/// </summary>
public sealed record GetPoolMetricsQuery : IQuery<LiquidityMetrics>
{
    /// <summary>
    /// The pool address.
    /// </summary>
    public required string PoolAddress { get; init; }
}
