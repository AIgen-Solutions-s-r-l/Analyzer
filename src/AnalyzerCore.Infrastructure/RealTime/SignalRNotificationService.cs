using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Infrastructure.RealTime;

/// <summary>
/// SignalR-based implementation of real-time notifications.
/// Generic implementation that works with any Hub type.
/// </summary>
public class SignalRNotificationService<THub> : IRealtimeNotificationService where THub : Hub
{
    private readonly IHubContext<THub> _hubContext;
    private readonly ILogger<SignalRNotificationService<THub>> _logger;

    public SignalRNotificationService(
        IHubContext<THub> hubContext,
        ILogger<SignalRNotificationService<THub>> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewBlockAsync(long blockNumber, string blockHash, DateTime timestamp)
    {
        var message = new
        {
            BlockNumber = blockNumber,
            BlockHash = blockHash,
            Timestamp = timestamp,
            Type = "NewBlock"
        };

        await _hubContext.Clients.Group("blocks").SendAsync("BlockUpdate", message);

        _logger.LogDebug("Broadcasted new block {BlockNumber}", blockNumber);
    }

    public async Task NotifyPoolUpdateAsync(Pool pool)
    {
        var message = new
        {
            pool.Address,
            pool.Token0Address,
            pool.Token1Address,
            pool.Reserve0,
            pool.Reserve1,
            pool.Factory,
            UpdatedAt = DateTime.UtcNow,
            Type = "PoolUpdate"
        };

        var groupName = $"pool:{pool.Address.ToLowerInvariant()}";
        await _hubContext.Clients.Group(groupName).SendAsync("PoolUpdate", message);

        // Also notify token subscribers
        await _hubContext.Clients.Group($"token:{pool.Token0Address.ToLowerInvariant()}")
            .SendAsync("TokenPoolUpdate", message);
        await _hubContext.Clients.Group($"token:{pool.Token1Address.ToLowerInvariant()}")
            .SendAsync("TokenPoolUpdate", message);

        _logger.LogDebug("Broadcasted pool update for {PoolAddress}", pool.Address);
    }

    public async Task NotifyPriceUpdateAsync(TokenPrice price)
    {
        var message = new
        {
            price.TokenAddress,
            price.QuoteTokenAddress,
            price.QuoteTokenSymbol,
            price.Price,
            price.PriceUsd,
            price.PoolAddress,
            price.Liquidity,
            price.Timestamp,
            Type = "PriceUpdate"
        };

        var groupName = $"token:{price.TokenAddress.ToLowerInvariant()}";
        await _hubContext.Clients.Group(groupName).SendAsync("PriceUpdate", message);

        _logger.LogDebug(
            "Broadcasted price update for {TokenAddress}: {Price}",
            price.TokenAddress,
            price.Price);
    }

    public async Task NotifyNewTokenAsync(Token token)
    {
        var message = new
        {
            token.Address,
            token.Symbol,
            token.Name,
            token.Decimals,
            token.ChainId,
            token.CreatedAt,
            Type = "NewToken"
        };

        await _hubContext.Clients.Group("new-tokens").SendAsync("NewToken", message);

        _logger.LogDebug("Broadcasted new token: {Symbol} ({Address})", token.Symbol, token.Address);
    }

    public async Task NotifyNewPoolAsync(Pool pool)
    {
        var message = new
        {
            pool.Address,
            pool.Token0Address,
            pool.Token1Address,
            pool.Factory,
            pool.Reserve0,
            pool.Reserve1,
            pool.CreatedAt,
            Type = "NewPool"
        };

        await _hubContext.Clients.Group("new-pools").SendAsync("NewPool", message);

        _logger.LogDebug("Broadcasted new pool: {Address}", pool.Address);
    }

    public async Task NotifyArbitrageOpportunityAsync(ArbitrageOpportunity opportunity)
    {
        var message = new
        {
            opportunity.Id,
            opportunity.TokenAddress,
            opportunity.TokenSymbol,
            opportunity.BuyPrice,
            opportunity.SellPrice,
            opportunity.SpreadPercent,
            opportunity.ExpectedProfitUsd,
            opportunity.NetProfitUsd,
            opportunity.RoiPercent,
            opportunity.IsProfitable,
            opportunity.ConfidenceScore,
            opportunity.DetectedAt,
            PathLength = opportunity.Path.Count,
            Type = "ArbitrageOpportunity"
        };

        // Send to all arbitrage subscribers
        await _hubContext.Clients.Group("arbitrage:all").SendAsync("ArbitrageOpportunity", message);

        // Send to threshold-based groups
        var profitThresholds = new[] { 10m, 50m, 100m, 500m, 1000m };
        foreach (var threshold in profitThresholds)
        {
            if (opportunity.NetProfitUsd >= threshold)
            {
                await _hubContext.Clients.Group($"arbitrage:min{threshold:F0}")
                    .SendAsync("ArbitrageOpportunity", message);
            }
        }

        _logger.LogDebug(
            "Broadcasted arbitrage opportunity: {TokenSymbol} ${NetProfit:F2}",
            opportunity.TokenSymbol,
            opportunity.NetProfitUsd);
    }

    public async Task NotifySignificantPriceChangeAsync(
        string tokenAddress,
        string tokenSymbol,
        decimal oldPrice,
        decimal newPrice,
        decimal changePercent)
    {
        var message = new
        {
            TokenAddress = tokenAddress.ToLowerInvariant(),
            TokenSymbol = tokenSymbol,
            OldPrice = oldPrice,
            NewPrice = newPrice,
            ChangePercent = changePercent,
            Direction = changePercent > 0 ? "up" : "down",
            Timestamp = DateTime.UtcNow,
            Type = "SignificantPriceChange"
        };

        var groupName = $"token:{tokenAddress.ToLowerInvariant()}";
        await _hubContext.Clients.Group(groupName).SendAsync("SignificantPriceChange", message);

        _logger.LogInformation(
            "Broadcasted significant price change for {TokenSymbol}: {ChangePercent:F2}%",
            tokenSymbol,
            changePercent);
    }
}
