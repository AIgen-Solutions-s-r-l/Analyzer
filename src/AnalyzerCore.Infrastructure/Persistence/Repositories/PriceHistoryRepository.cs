using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for price history data.
/// </summary>
public class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PriceHistoryRepository> _logger;

    public PriceHistoryRepository(
        ApplicationDbContext context,
        ILogger<PriceHistoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(PriceHistory priceHistory, CancellationToken cancellationToken = default)
    {
        await _context.PriceHistories.AddAsync(priceHistory, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Added price history for {TokenAddress} at {Timestamp}: {Price}",
            priceHistory.TokenAddress,
            priceHistory.Timestamp,
            priceHistory.Price);
    }

    public async Task AddRangeAsync(IEnumerable<PriceHistory> priceHistories, CancellationToken cancellationToken = default)
    {
        var historyList = priceHistories.ToList();
        if (!historyList.Any())
            return;

        await _context.PriceHistories.AddRangeAsync(historyList, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Added {Count} price history entries", historyList.Count);
    }

    public async Task<IReadOnlyList<PriceHistory>> GetByTokenAsync(
        string tokenAddress,
        string? quoteTokenSymbol = null,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var normalizedAddress = tokenAddress.ToLowerInvariant();

        var query = _context.PriceHistories
            .AsNoTracking()
            .Where(p => p.TokenAddress == normalizedAddress);

        if (!string.IsNullOrEmpty(quoteTokenSymbol))
        {
            var normalizedQuote = quoteTokenSymbol.ToUpperInvariant();
            query = query.Where(p => p.QuoteTokenSymbol == normalizedQuote);
        }

        if (from.HasValue)
        {
            query = query.Where(p => p.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(p => p.Timestamp <= to.Value);
        }

        return await query
            .OrderByDescending(p => p.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<PriceHistory?> GetLatestAsync(
        string tokenAddress,
        string? quoteTokenSymbol = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedAddress = tokenAddress.ToLowerInvariant();

        var query = _context.PriceHistories
            .AsNoTracking()
            .Where(p => p.TokenAddress == normalizedAddress);

        if (!string.IsNullOrEmpty(quoteTokenSymbol))
        {
            var normalizedQuote = quoteTokenSymbol.ToUpperInvariant();
            query = query.Where(p => p.QuoteTokenSymbol == normalizedQuote);
        }

        return await query
            .OrderByDescending(p => p.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceHistory>> GetForTwapAsync(
        string tokenAddress,
        string quoteTokenSymbol,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var normalizedAddress = tokenAddress.ToLowerInvariant();
        var normalizedQuote = quoteTokenSymbol.ToUpperInvariant();

        return await _context.PriceHistories
            .AsNoTracking()
            .Where(p => p.TokenAddress == normalizedAddress &&
                       p.QuoteTokenSymbol == normalizedQuote &&
                       p.Timestamp >= from &&
                       p.Timestamp <= to)
            .OrderBy(p => p.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteOlderThanAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default)
    {
        var oldRecords = await _context.PriceHistories
            .Where(p => p.Timestamp < cutoffDate)
            .ToListAsync(cancellationToken);

        if (!oldRecords.Any())
            return 0;

        _context.PriceHistories.RemoveRange(oldRecords);
        var deleted = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted {Count} price history records older than {CutoffDate}",
            deleted,
            cutoffDate);

        return deleted;
    }
}
