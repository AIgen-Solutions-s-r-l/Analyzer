using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Liquidity.Queries.GetTopPools;

/// <summary>
/// Query to get top pools by TVL.
/// </summary>
public sealed record GetTopPoolsQuery : IQuery<IReadOnlyList<LiquidityMetrics>>
{
    /// <summary>
    /// Maximum number of pools to return.
    /// </summary>
    public int Limit { get; init; } = 10;
}
