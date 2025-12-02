using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Prices.Queries.GetTwap;

/// <summary>
/// Query to get the Time-Weighted Average Price (TWAP) for a token.
/// </summary>
public sealed record GetTwapQuery : IQuery<TwapResult>
{
    /// <summary>
    /// The token address to calculate TWAP for.
    /// </summary>
    public required string TokenAddress { get; init; }

    /// <summary>
    /// The quote currency (e.g., "ETH", "USDC").
    /// </summary>
    public string QuoteCurrency { get; init; } = "ETH";

    /// <summary>
    /// The time period for TWAP calculation in minutes.
    /// </summary>
    public int PeriodMinutes { get; init; } = 60;
}
