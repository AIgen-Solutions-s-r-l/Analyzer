using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Domain.Services;

/// <summary>
/// Service interface for price discovery and calculation.
/// </summary>
public interface IPriceService
{
    /// <summary>
    /// Gets the current price of a token in the specified quote currency.
    /// </summary>
    Task<Result<TokenPrice>> GetTokenPriceAsync(
        string tokenAddress,
        string quoteCurrency = "ETH",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current price of a token in USD.
    /// </summary>
    Task<Result<TokenPrice>> GetTokenPriceUsdAsync(
        string tokenAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates TWAP for a token over the specified period.
    /// </summary>
    Task<Result<TwapResult>> GetTwapAsync(
        string tokenAddress,
        string quoteCurrency = "ETH",
        TimeSpan? period = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical prices for a token.
    /// </summary>
    Task<Result<IReadOnlyList<TokenPrice>>> GetPriceHistoryAsync(
        string tokenAddress,
        string quoteCurrency = "ETH",
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates price from pool reserves.
    /// </summary>
    decimal CalculatePriceFromReserves(
        decimal reserve0,
        decimal reserve1,
        int decimals0,
        int decimals1,
        bool isToken0);

    /// <summary>
    /// Gets supported quote currencies.
    /// </summary>
    IReadOnlyList<string> GetSupportedQuoteCurrencies();
}
