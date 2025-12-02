using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Prices.Queries.GetTokenPrice;

/// <summary>
/// Query to get the current price of a token.
/// </summary>
public sealed record GetTokenPriceQuery : IQuery<TokenPrice>
{
    /// <summary>
    /// The token address to get the price for.
    /// </summary>
    public required string TokenAddress { get; init; }

    /// <summary>
    /// The quote currency (e.g., "ETH", "USDC", "USD").
    /// </summary>
    public string QuoteCurrency { get; init; } = "ETH";
}
