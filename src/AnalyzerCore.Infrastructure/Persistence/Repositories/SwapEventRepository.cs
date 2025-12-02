using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for swap event persistence.
/// </summary>
public class SwapEventRepository : ISwapEventRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SwapEventRepository> _logger;

    public SwapEventRepository(
        ApplicationDbContext context,
        ILogger<SwapEventRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(SwapEvent swapEvent, CancellationToken cancellationToken = default)
    {
        await _context.SwapEvents.AddAsync(swapEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<SwapEvent> swapEvents, CancellationToken cancellationToken = default)
    {
        var eventList = swapEvents.ToList();
        if (!eventList.Any()) return;

        await _context.SwapEvents.AddRangeAsync(eventList, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added {Count} swap events to database", eventList.Count);
    }

    public async Task<decimal> GetPoolVolumeAsync(
        string poolAddress,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var normalizedAddress = poolAddress.ToLowerInvariant();

        return await _context.SwapEvents
            .AsNoTracking()
            .Where(s => s.PoolAddress == normalizedAddress &&
                       s.Timestamp >= from &&
                       s.Timestamp <= to)
            .SumAsync(s => Math.Abs(s.AmountUsd), cancellationToken);
    }

    public async Task<decimal> GetTokenVolumeAsync(
        string tokenAddress,
        string chainId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var normalizedAddress = tokenAddress.ToLowerInvariant();

        // Get all pools containing this token
        var poolAddresses = await _context.Pools
            .AsNoTracking()
            .Where(p => (p.Token0.Address == normalizedAddress || p.Token1.Address == normalizedAddress) &&
                       p.Token0.ChainId == chainId)
            .Select(p => p.Address)
            .ToListAsync(cancellationToken);

        if (!poolAddresses.Any())
            return 0;

        // Sum volume from all pools (divide by 2 to avoid double counting)
        var totalVolume = await _context.SwapEvents
            .AsNoTracking()
            .Where(s => poolAddresses.Contains(s.PoolAddress) &&
                       s.Timestamp >= from &&
                       s.Timestamp <= to)
            .SumAsync(s => Math.Abs(s.AmountUsd), cancellationToken);

        return totalVolume;
    }

    public async Task<IReadOnlyList<SwapEvent>> GetRecentSwapsAsync(
        string poolAddress,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var normalizedAddress = poolAddress.ToLowerInvariant();

        return await _context.SwapEvents
            .AsNoTracking()
            .Where(s => s.PoolAddress == normalizedAddress)
            .OrderByDescending(s => s.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SwapEvent>> GetSwapsInRangeAsync(
        string poolAddress,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var normalizedAddress = poolAddress.ToLowerInvariant();

        return await _context.SwapEvents
            .AsNoTracking()
            .Where(s => s.PoolAddress == normalizedAddress &&
                       s.Timestamp >= from &&
                       s.Timestamp <= to)
            .OrderByDescending(s => s.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string transactionHash,
        int logIndex,
        CancellationToken cancellationToken = default)
    {
        var normalizedHash = transactionHash.ToLowerInvariant();

        return await _context.SwapEvents
            .AsNoTracking()
            .AnyAsync(s => s.TransactionHash == normalizedHash &&
                          s.LogIndex == logIndex,
                     cancellationToken);
    }

    public async Task<int> DeleteOlderThanAsync(
        DateTime threshold,
        CancellationToken cancellationToken = default)
    {
        var count = await _context.SwapEvents
            .Where(s => s.Timestamp < threshold)
            .ExecuteDeleteAsync(cancellationToken);

        if (count > 0)
        {
            _logger.LogInformation(
                "Deleted {Count} swap events older than {Threshold}",
                count,
                threshold);
        }

        return count;
    }
}
