using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Arbitrage.Queries.ScanArbitrage;

/// <summary>
/// Query to scan for arbitrage opportunities across all pools.
/// </summary>
public sealed record ScanArbitrageQuery : IQuery<IReadOnlyList<ArbitrageOpportunity>>
{
    /// <summary>
    /// Minimum profit threshold in USD.
    /// </summary>
    public decimal MinProfitUsd { get; init; } = 10m;
}
