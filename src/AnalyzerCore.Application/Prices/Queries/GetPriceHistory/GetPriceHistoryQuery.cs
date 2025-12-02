using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Prices.Queries.GetPriceHistory;

/// <summary>
/// Query to get historical prices for a token.
/// </summary>
public sealed record GetPriceHistoryQuery : IQuery<IReadOnlyList<TokenPrice>>
{
    /// <summary>
    /// The token address to get price history for.
    /// </summary>
    public required string TokenAddress { get; init; }

    /// <summary>
    /// The quote currency (e.g., "ETH", "USDC").
    /// </summary>
    public string QuoteCurrency { get; init; } = "ETH";

    /// <summary>
    /// Start of the time range (optional).
    /// </summary>
    public DateTime? From { get; init; }

    /// <summary>
    /// End of the time range (optional).
    /// </summary>
    public DateTime? To { get; init; }

    /// <summary>
    /// Maximum number of records to return.
    /// </summary>
    public int Limit { get; init; } = 100;
}
