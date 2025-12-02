using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Domain.Repositories;

/// <summary>
/// Repository interface for price history data.
/// </summary>
public interface IPriceHistoryRepository
{
    /// <summary>
    /// Adds a new price history entry.
    /// </summary>
    Task AddAsync(PriceHistory priceHistory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple price history entries.
    /// </summary>
    Task AddRangeAsync(IEnumerable<PriceHistory> priceHistories, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets price history for a token.
    /// </summary>
    Task<IReadOnlyList<PriceHistory>> GetByTokenAsync(
        string tokenAddress,
        string? quoteTokenSymbol = null,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest price for a token.
    /// </summary>
    Task<PriceHistory?> GetLatestAsync(
        string tokenAddress,
        string? quoteTokenSymbol = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets price history within a time range for TWAP calculation.
    /// </summary>
    Task<IReadOnlyList<PriceHistory>> GetForTwapAsync(
        string tokenAddress,
        string quoteTokenSymbol,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old price history entries.
    /// </summary>
    Task<int> DeleteOlderThanAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default);
}
