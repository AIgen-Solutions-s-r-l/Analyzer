using AnalyzerCore.Application.Abstractions.Caching;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using AnalyzerCore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Volume calculation constants
using VolumeWindow = System.TimeSpan;

namespace AnalyzerCore.Infrastructure.Services;

/// <summary>
/// Service for liquidity analytics and metrics calculation.
/// </summary>
public sealed class LiquidityAnalyticsService : ILiquidityAnalyticsService
{
    private readonly IPoolRepository _poolRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly ISwapEventRepository _swapEventRepository;
    private readonly IPriceService _priceService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<LiquidityAnalyticsService> _logger;
    private readonly string _chainId;

    // DEX fee (0.3% for Uniswap V2 style)
    private const decimal DefaultFeePercent = 0.3m;

    // Volume calculation window
    private static readonly VolumeWindow Volume24hWindow = TimeSpan.FromHours(24);

    public LiquidityAnalyticsService(
        IPoolRepository poolRepository,
        ITokenRepository tokenRepository,
        ISwapEventRepository swapEventRepository,
        IPriceService priceService,
        ICacheService cacheService,
        IOptions<BlockchainOptions> blockchainOptions,
        ILogger<LiquidityAnalyticsService> logger)
    {
        _poolRepository = poolRepository;
        _tokenRepository = tokenRepository;
        _swapEventRepository = swapEventRepository;
        _priceService = priceService;
        _cacheService = cacheService;
        _chainId = blockchainOptions.Value.ChainId;
        _logger = logger;
    }

    public async Task<Result<LiquidityMetrics>> GetPoolMetricsAsync(
        string poolAddress,
        CancellationToken cancellationToken = default)
    {
        poolAddress = poolAddress.ToLowerInvariant();
        var cacheKey = $"liquidity:pool:{poolAddress}";

        var cached = await _cacheService.GetAsync<LiquidityMetrics>(cacheKey);
        if (cached != null)
        {
            return Result.Success(cached);
        }

        // Get pool
        var pool = await _poolRepository.GetByAddressAsync(poolAddress, "", cancellationToken);
        if (pool == null)
        {
            return Result.Failure<LiquidityMetrics>(
                new Error("Liquidity.PoolNotFound", $"Pool {poolAddress} not found"));
        }

        // Get tokens
        var token0 = await _tokenRepository.GetByAddressAsync(pool.Token0Address, _chainId, cancellationToken);
        var token1 = await _tokenRepository.GetByAddressAsync(pool.Token1Address, _chainId, cancellationToken);

        // Get USD prices
        var price0Result = await _priceService.GetTokenPriceUsdAsync(pool.Token0Address, cancellationToken);
        var price1Result = await _priceService.GetTokenPriceUsdAsync(pool.Token1Address, cancellationToken);

        var price0Usd = price0Result.IsSuccess ? price0Result.Value.PriceUsd : 0;
        var price1Usd = price1Result.IsSuccess ? price1Result.Value.PriceUsd : 0;

        // Calculate reserves in USD
        var reserve0Usd = pool.Reserve0 * price0Usd;
        var reserve1Usd = pool.Reserve1 * price1Usd;

        // Calculate 24h volume from swap events
        var now = DateTime.UtcNow;
        var volume24hUsd = await _swapEventRepository.GetPoolVolumeAsync(
            poolAddress,
            now - Volume24hWindow,
            now,
            cancellationToken);

        var metrics = LiquidityMetrics.Create(
            pool.Address,
            pool.Token0Address,
            token0?.Symbol ?? "UNKNOWN",
            pool.Token1Address,
            token1?.Symbol ?? "UNKNOWN",
            pool.Reserve0,
            pool.Reserve1,
            reserve0Usd,
            reserve1Usd,
            volume24hUsd,
            DefaultFeePercent);

        // Cache for 1 minute
        await _cacheService.SetAsync(cacheKey, metrics, TimeSpan.FromMinutes(1));

        return Result.Success(metrics);
    }

    public async Task<Result<TokenLiquiditySummary>> GetTokenLiquiditySummaryAsync(
        string tokenAddress,
        CancellationToken cancellationToken = default)
    {
        tokenAddress = tokenAddress.ToLowerInvariant();
        var cacheKey = $"liquidity:token:{tokenAddress}";

        var cached = await _cacheService.GetAsync<TokenLiquiditySummary>(cacheKey);
        if (cached != null)
        {
            return Result.Success(cached);
        }

        // Get token info
        var token = await _tokenRepository.GetByAddressAsync(tokenAddress, _chainId, cancellationToken);
        if (token == null)
        {
            return Result.Failure<TokenLiquiditySummary>(
                new Error("Liquidity.TokenNotFound", $"Token {tokenAddress} not found"));
        }

        // Get all pools containing this token
        var pools = await _poolRepository.GetPoolsByTokenAsync(tokenAddress, _chainId, cancellationToken);
        var poolList = pools.ToList();

        if (!poolList.Any())
        {
            return Result.Success(new TokenLiquiditySummary
            {
                TokenAddress = tokenAddress,
                TokenSymbol = token.Symbol,
                TotalLiquidityUsd = 0,
                PoolCount = 0,
                TopPools = Array.Empty<PoolLiquiditySummary>(),
                AverageLiquidityPerPool = 0,
                TotalVolume24hUsd = 0,
                Timestamp = DateTime.UtcNow
            });
        }

        // Calculate liquidity for each pool
        var poolMetrics = new List<(string PoolAddress, string PairedToken, string PairedSymbol, decimal LiquidityUsd)>();
        decimal totalLiquidityUsd = 0;

        foreach (var pool in poolList)
        {
            var metricsResult = await GetPoolMetricsAsync(pool.Address, cancellationToken);
            if (metricsResult.IsFailure) continue;

            var metrics = metricsResult.Value;
            var isToken0 = pool.Token0Address.Equals(tokenAddress, StringComparison.OrdinalIgnoreCase);

            var pairedToken = isToken0 ? pool.Token1Address : pool.Token0Address;
            var pairedSymbol = isToken0 ? metrics.Token1Symbol : metrics.Token0Symbol;
            var poolLiquidity = metrics.TvlUsd;

            poolMetrics.Add((pool.Address, pairedToken, pairedSymbol, poolLiquidity));
            totalLiquidityUsd += poolLiquidity;
        }

        // Sort by liquidity and create top pools list
        var sortedPools = poolMetrics
            .OrderByDescending(p => p.LiquidityUsd)
            .Take(10)
            .Select(p => new PoolLiquiditySummary
            {
                PoolAddress = p.PoolAddress,
                PairedTokenAddress = p.PairedToken,
                PairedTokenSymbol = p.PairedSymbol,
                LiquidityUsd = p.LiquidityUsd,
                SharePercent = totalLiquidityUsd > 0 ? (p.LiquidityUsd / totalLiquidityUsd) * 100 : 0
            })
            .ToList();

        var summary = new TokenLiquiditySummary
        {
            TokenAddress = tokenAddress,
            TokenSymbol = token.Symbol,
            TotalLiquidityUsd = totalLiquidityUsd,
            PoolCount = poolList.Count,
            TopPools = sortedPools,
            AverageLiquidityPerPool = poolList.Count > 0 ? totalLiquidityUsd / poolList.Count : 0,
            TotalVolume24hUsd = await GetTokenVolume24hAsync(tokenAddress, cancellationToken),
            Timestamp = DateTime.UtcNow
        };

        // Cache for 2 minutes
        await _cacheService.SetAsync(cacheKey, summary, TimeSpan.FromMinutes(2));

        return Result.Success(summary);
    }

    public async Task<Result<IReadOnlyList<LiquidityMetrics>>> GetTopPoolsByTvlAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"liquidity:top:{limit}";

        var cached = await _cacheService.GetAsync<List<LiquidityMetrics>>(cacheKey);
        if (cached != null)
        {
            return Result.Success<IReadOnlyList<LiquidityMetrics>>(cached);
        }

        // Get all pools
        var pools = await _poolRepository.GetAllByChainIdAsync(_chainId, cancellationToken);
        var poolList = pools.ToList();

        var metricsWithTvl = new List<LiquidityMetrics>();

        foreach (var pool in poolList)
        {
            var metricsResult = await GetPoolMetricsAsync(pool.Address, cancellationToken);
            if (metricsResult.IsSuccess && metricsResult.Value.TvlUsd > 0)
            {
                metricsWithTvl.Add(metricsResult.Value);
            }
        }

        var topPools = metricsWithTvl
            .OrderByDescending(m => m.TvlUsd)
            .Take(limit)
            .ToList();

        // Cache for 5 minutes
        await _cacheService.SetAsync(cacheKey, topPools, TimeSpan.FromMinutes(5));

        return Result.Success<IReadOnlyList<LiquidityMetrics>>(topPools);
    }

    public async Task<Result<ImpermanentLossResult>> CalculateImpermanentLossAsync(
        string poolAddress,
        decimal entryPriceRatio,
        decimal initialInvestmentUsd,
        CancellationToken cancellationToken = default)
    {
        poolAddress = poolAddress.ToLowerInvariant();

        // Get pool
        var pool = await _poolRepository.GetByAddressAsync(poolAddress, "", cancellationToken);
        if (pool == null)
        {
            return Result.Failure<ImpermanentLossResult>(
                new Error("Liquidity.PoolNotFound", $"Pool {poolAddress} not found"));
        }

        // Get current prices
        var price0Result = await _priceService.GetTokenPriceUsdAsync(pool.Token0Address, cancellationToken);
        var price1Result = await _priceService.GetTokenPriceUsdAsync(pool.Token1Address, cancellationToken);

        if (price0Result.IsFailure || price1Result.IsFailure)
        {
            return Result.Failure<ImpermanentLossResult>(
                new Error("Liquidity.PriceUnavailable", "Unable to get token prices"));
        }

        // Current price ratio
        var currentPriceRatio = price0Result.Value.PriceUsd > 0
            ? price1Result.Value.PriceUsd / price0Result.Value.PriceUsd
            : 0;

        if (currentPriceRatio <= 0)
        {
            return Result.Failure<ImpermanentLossResult>(
                new Error("Liquidity.InvalidPrice", "Invalid price ratio"));
        }

        var result = ImpermanentLossResult.Calculate(
            poolAddress,
            entryPriceRatio,
            currentPriceRatio,
            initialInvestmentUsd);

        return Result.Success(result);
    }

    public Task<Result<IReadOnlyList<TvlDataPoint>>> GetHistoricalTvlAsync(
        string poolAddress,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        // Historical TVL would require event indexing
        // Return empty for now
        return Task.FromResult(Result.Success<IReadOnlyList<TvlDataPoint>>(
            Array.Empty<TvlDataPoint>()));
    }

    public async Task<Result<LiquidityConcentration>> GetLiquidityConcentrationAsync(
        string tokenAddress,
        CancellationToken cancellationToken = default)
    {
        tokenAddress = tokenAddress.ToLowerInvariant();

        var summaryResult = await GetTokenLiquiditySummaryAsync(tokenAddress, cancellationToken);
        if (summaryResult.IsFailure)
        {
            return Result.Failure<LiquidityConcentration>(summaryResult.Error);
        }

        var summary = summaryResult.Value;
        if (summary.PoolCount == 0 || summary.TotalLiquidityUsd == 0)
        {
            return Result.Success(new LiquidityConcentration
            {
                TokenAddress = tokenAddress,
                TotalLiquidityUsd = 0,
                TopPoolConcentration = 0,
                Top3PoolsConcentration = 0,
                Top5PoolsConcentration = 0,
                HhiIndex = 0,
                ConcentrationLevel = "No Liquidity",
                PoolCount = 0
            });
        }

        // Calculate concentrations
        var topPoolConcentration = summary.TopPools.FirstOrDefault()?.SharePercent ?? 0;
        var top3Concentration = summary.TopPools.Take(3).Sum(p => p.SharePercent);
        var top5Concentration = summary.TopPools.Take(5).Sum(p => p.SharePercent);

        // Calculate HHI (Herfindahl-Hirschman Index)
        var hhi = summary.TopPools.Sum(p => p.SharePercent * p.SharePercent);

        // Determine concentration level
        var concentrationLevel = hhi switch
        {
            >= 2500 => "Highly Concentrated",
            >= 1500 => "Moderately Concentrated",
            >= 1000 => "Moderately Competitive",
            _ => "Competitive"
        };

        return Result.Success(new LiquidityConcentration
        {
            TokenAddress = tokenAddress,
            TotalLiquidityUsd = summary.TotalLiquidityUsd,
            TopPoolConcentration = topPoolConcentration,
            Top3PoolsConcentration = top3Concentration,
            Top5PoolsConcentration = top5Concentration,
            HhiIndex = hhi,
            ConcentrationLevel = concentrationLevel,
            PoolCount = summary.PoolCount
        });
    }

    /// <summary>
    /// Gets the 24h trading volume for a token across all pools.
    /// </summary>
    private async Task<decimal> GetTokenVolume24hAsync(
        string tokenAddress,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"volume:token:{tokenAddress}:24h";
        var cached = await _cacheService.GetAsync<decimal?>(cacheKey);
        if (cached.HasValue)
        {
            return cached.Value;
        }

        var now = DateTime.UtcNow;
        var volume = await _swapEventRepository.GetTokenVolumeAsync(
            tokenAddress,
            _chainId,
            now - Volume24hWindow,
            now,
            cancellationToken);

        // Cache for 5 minutes
        await _cacheService.SetAsync(cacheKey, volume, TimeSpan.FromMinutes(5));

        return volume;
    }
}
