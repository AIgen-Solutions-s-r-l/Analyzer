using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Domain.Repositories;

/// <summary>
/// Repository for swap event persistence and querying.
/// </summary>
public interface ISwapEventRepository
{
    /// <summary>
    /// Adds a new swap event.
    /// </summary>
    Task AddAsync(SwapEvent swapEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple swap events in a batch.
    /// </summary>
    Task AddRangeAsync(IEnumerable<SwapEvent> swapEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total volume for a pool within a time range.
    /// </summary>
    Task<decimal> GetPoolVolumeAsync(
        string poolAddress,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total volume for a token across all pools within a time range.
    /// </summary>
    Task<decimal> GetTokenVolumeAsync(
        string tokenAddress,
        string chainId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent swap events for a pool.
    /// </summary>
    Task<IReadOnlyList<SwapEvent>> GetRecentSwapsAsync(
        string poolAddress,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets swap events within a time range for a pool.
    /// </summary>
    Task<IReadOnlyList<SwapEvent>> GetSwapsInRangeAsync(
        string poolAddress,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a swap event already exists (for deduplication).
    /// </summary>
    Task<bool> ExistsAsync(
        string transactionHash,
        int logIndex,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old swap events for cleanup.
    /// </summary>
    Task<int> DeleteOlderThanAsync(
        DateTime threshold,
        CancellationToken cancellationToken = default);
}
