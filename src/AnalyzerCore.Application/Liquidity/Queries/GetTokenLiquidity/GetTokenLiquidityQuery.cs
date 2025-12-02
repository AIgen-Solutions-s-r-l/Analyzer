using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Liquidity.Queries.GetTokenLiquidity;

/// <summary>
/// Query to get liquidity summary for a token.
/// </summary>
public sealed record GetTokenLiquidityQuery : IQuery<TokenLiquiditySummary>
{
    /// <summary>
    /// The token address.
    /// </summary>
    public required string TokenAddress { get; init; }
}
