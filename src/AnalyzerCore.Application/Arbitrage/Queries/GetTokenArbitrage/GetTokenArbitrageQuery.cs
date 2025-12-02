using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Arbitrage.Queries.GetTokenArbitrage;

/// <summary>
/// Query to find arbitrage opportunities for a specific token.
/// </summary>
public sealed record GetTokenArbitrageQuery : IQuery<IReadOnlyList<ArbitrageOpportunity>>
{
    /// <summary>
    /// The token address to find arbitrage opportunities for.
    /// </summary>
    public required string TokenAddress { get; init; }
}
