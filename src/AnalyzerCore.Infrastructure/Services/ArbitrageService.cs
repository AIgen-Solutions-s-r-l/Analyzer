using AnalyzerCore.Application.Abstractions.Caching;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using AnalyzerCore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyzerCore.Infrastructure.Services;

/// <summary>
/// Service for detecting and analyzing arbitrage opportunities between DEX pools.
/// </summary>
public sealed class ArbitrageService : IArbitrageService
{
    private readonly IPoolRepository _poolRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IPriceService _priceService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ArbitrageService> _logger;
    private readonly string _chainId;

    // WETH address for Ethereum mainnet
    private const string WethAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";

    // Minimum liquidity to consider a pool (in USD)
    private const decimal MinLiquidityUsd = 10_000m;

    // Base gas cost for a swap (in gas units)
    private const int BaseSwapGas = 150_000;

    // Current gas price assumption (in Gwei) - should be fetched from chain
    private const decimal DefaultGasPriceGwei = 30m;

    public ArbitrageService(
        IPoolRepository poolRepository,
        ITokenRepository tokenRepository,
        IPriceService priceService,
        ICacheService cacheService,
        IOptions<BlockchainOptions> blockchainOptions,
        ILogger<ArbitrageService> logger)
    {
        _poolRepository = poolRepository;
        _tokenRepository = tokenRepository;
        _priceService = priceService;
        _cacheService = cacheService;
        _chainId = blockchainOptions.Value.ChainId;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<ArbitrageOpportunity>>> ScanForOpportunitiesAsync(
        decimal minProfitUsd = 10m,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"arbitrage:scan:{minProfitUsd}";
        var cached = await _cacheService.GetAsync<List<ArbitrageOpportunity>>(cacheKey);
        if (cached != null)
        {
            return Result.Success<IReadOnlyList<ArbitrageOpportunity>>(cached);
        }

        var opportunities = new List<ArbitrageOpportunity>();

        // Get all pools
        var pools = await _poolRepository.GetAllByChainIdAsync(_chainId, cancellationToken);
        var poolList = pools.ToList();

        _logger.LogDebug("Scanning {PoolCount} pools for arbitrage opportunities", poolList.Count);

        // Group pools by token pairs
        var poolsByPair = poolList
            .GroupBy(p => GetNormalizedPairKey(p.Token0Address, p.Token1Address))
            .Where(g => g.Count() >= 2) // Need at least 2 pools for arbitrage
            .ToList();

        foreach (var pairGroup in poolsByPair)
        {
            var pairPools = pairGroup.OrderByDescending(p => p.Reserve0 + p.Reserve1).ToList();

            // Compare each pool pair for price discrepancies
            for (int i = 0; i < pairPools.Count - 1; i++)
            {
                for (int j = i + 1; j < pairPools.Count; j++)
                {
                    var opportunity = await AnalyzePoolPairAsync(
                        pairPools[i],
                        pairPools[j],
                        cancellationToken);

                    if (opportunity != null && opportunity.NetProfitUsd >= minProfitUsd)
                    {
                        opportunities.Add(opportunity);
                    }
                }
            }
        }

        // Sort by profit
        var sorted = opportunities.OrderByDescending(o => o.NetProfitUsd).ToList();

        // Cache for 10 seconds (arbitrage data is time-sensitive)
        await _cacheService.SetAsync(cacheKey, sorted, TimeSpan.FromSeconds(10));

        _logger.LogInformation(
            "Found {Count} arbitrage opportunities above ${MinProfit}",
            sorted.Count,
            minProfitUsd);

        return Result.Success<IReadOnlyList<ArbitrageOpportunity>>(sorted);
    }

    public async Task<Result<IReadOnlyList<ArbitrageOpportunity>>> FindOpportunitiesForTokenAsync(
        string tokenAddress,
        CancellationToken cancellationToken = default)
    {
        tokenAddress = tokenAddress.ToLowerInvariant();

        var pools = await _poolRepository.GetPoolsByTokenAsync(tokenAddress, _chainId, cancellationToken);
        var poolList = pools.ToList();

        if (poolList.Count < 2)
        {
            return Result.Success<IReadOnlyList<ArbitrageOpportunity>>(Array.Empty<ArbitrageOpportunity>());
        }

        var opportunities = new List<ArbitrageOpportunity>();

        // Compare all pool pairs
        for (int i = 0; i < poolList.Count - 1; i++)
        {
            for (int j = i + 1; j < poolList.Count; j++)
            {
                var opportunity = await AnalyzePoolPairAsync(
                    poolList[i],
                    poolList[j],
                    cancellationToken);

                if (opportunity != null && opportunity.IsProfitable)
                {
                    opportunities.Add(opportunity);
                }
            }
        }

        return Result.Success<IReadOnlyList<ArbitrageOpportunity>>(
            opportunities.OrderByDescending(o => o.NetProfitUsd).ToList());
    }

    public async Task<Result<IReadOnlyList<ArbitrageOpportunity>>> FindTriangularOpportunitiesAsync(
        string baseToken,
        CancellationToken cancellationToken = default)
    {
        baseToken = baseToken.ToLowerInvariant();
        var opportunities = new List<ArbitrageOpportunity>();

        // Get all pools with the base token
        var basePools = await _poolRepository.GetPoolsByTokenAsync(baseToken, _chainId, cancellationToken);
        var basePoolList = basePools.ToList();

        foreach (var pool1 in basePoolList)
        {
            // Find the other token in this pool
            var intermediateToken = pool1.Token0Address.Equals(baseToken, StringComparison.OrdinalIgnoreCase)
                ? pool1.Token1Address
                : pool1.Token0Address;

            // Find pools with the intermediate token
            var intermediatePools = await _poolRepository.GetPoolsByTokenAsync(
                intermediateToken, _chainId, cancellationToken);

            foreach (var pool2 in intermediatePools)
            {
                if (pool2.Address.Equals(pool1.Address, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Find the third token
                var thirdToken = pool2.Token0Address.Equals(intermediateToken, StringComparison.OrdinalIgnoreCase)
                    ? pool2.Token1Address
                    : pool2.Token0Address;

                // Check if we can close the triangle back to base token
                var closingPools = await _poolRepository.GetPoolsByTokenAsync(
                    thirdToken, _chainId, cancellationToken);

                var closingPool = closingPools.FirstOrDefault(p =>
                    p.Token0Address.Equals(baseToken, StringComparison.OrdinalIgnoreCase) ||
                    p.Token1Address.Equals(baseToken, StringComparison.OrdinalIgnoreCase));

                if (closingPool != null)
                {
                    var opportunity = await AnalyzeTriangularArbitrageAsync(
                        pool1, pool2, closingPool,
                        baseToken, intermediateToken, thirdToken,
                        cancellationToken);

                    if (opportunity != null && opportunity.IsProfitable)
                    {
                        opportunities.Add(opportunity);
                    }
                }
            }
        }

        return Result.Success<IReadOnlyList<ArbitrageOpportunity>>(
            opportunities.OrderByDescending(o => o.NetProfitUsd).ToList());
    }

    public async Task<Result<(decimal OptimalInput, decimal ExpectedProfit)>> CalculateOptimalAmountAsync(
        string buyPool,
        string sellPool,
        string tokenAddress,
        CancellationToken cancellationToken = default)
    {
        var buyPoolEntity = await _poolRepository.GetByAddressAsync(buyPool, "", cancellationToken);
        var sellPoolEntity = await _poolRepository.GetByAddressAsync(sellPool, "", cancellationToken);

        if (buyPoolEntity == null || sellPoolEntity == null)
        {
            return Result.Failure<(decimal, decimal)>(
                new Error("Arbitrage.PoolNotFound", "One or more pools not found"));
        }

        // Calculate optimal amount using the constant product formula
        // For maximum profit: optimal_amount = sqrt(k_buy * k_sell / (price_buy * price_sell)) - reserve_buy
        var (buyPrice, sellPrice) = await GetPoolPricesAsync(
            buyPoolEntity, sellPoolEntity, tokenAddress, cancellationToken);

        if (buyPrice <= 0 || sellPrice <= 0 || buyPrice >= sellPrice)
        {
            return Result.Success((0m, 0m));
        }

        // Simplified optimal calculation
        var kBuy = buyPoolEntity.Reserve0 * buyPoolEntity.Reserve1;
        var kSell = sellPoolEntity.Reserve0 * sellPoolEntity.Reserve1;

        var optimalInput = (decimal)Math.Sqrt((double)(kBuy * kSell / (buyPrice * sellPrice))) -
                          GetReserveForToken(buyPoolEntity, tokenAddress);

        // Cap at a reasonable percentage of pool liquidity (max 10%)
        var maxInput = GetReserveForToken(buyPoolEntity, tokenAddress) * 0.1m;
        optimalInput = Math.Min(Math.Max(optimalInput, 0), maxInput);

        // Calculate expected profit
        var bought = CalculateAmountOut(optimalInput, buyPoolEntity, tokenAddress);
        var sold = CalculateAmountOut(bought, sellPoolEntity, tokenAddress);
        var profit = sold - optimalInput;

        return Result.Success((optimalInput, profit));
    }

    public async Task<Result<decimal>> EstimateGasCostAsync(
        ArbitrageOpportunity opportunity,
        CancellationToken cancellationToken = default)
    {
        // Get ETH price in USD
        var ethPriceResult = await _priceService.GetTokenPriceUsdAsync(WethAddress, cancellationToken);
        if (ethPriceResult.IsFailure)
        {
            // Use default estimate if price unavailable
            return Result.Success(50m); // Default $50 gas estimate
        }

        // Calculate gas cost
        var gasUnits = BaseSwapGas * opportunity.Path.Count;
        var gasCostEth = gasUnits * DefaultGasPriceGwei / 1_000_000_000m;
        var gasCostUsd = gasCostEth * ethPriceResult.Value.PriceUsd;

        return Result.Success(gasCostUsd);
    }

    public Task<Result<IReadOnlyList<ArbitrageOpportunity>>> GetHistoricalOpportunitiesAsync(
        DateTime? from = null,
        DateTime? to = null,
        decimal? minProfitUsd = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would query a database
        // For now, return empty list as we don't persist historical opportunities
        return Task.FromResult(Result.Success<IReadOnlyList<ArbitrageOpportunity>>(
            Array.Empty<ArbitrageOpportunity>()));
    }

    private async Task<ArbitrageOpportunity?> AnalyzePoolPairAsync(
        Domain.Entities.Pool pool1,
        Domain.Entities.Pool pool2,
        CancellationToken cancellationToken)
    {
        // Get token info
        var token0 = await _tokenRepository.GetByAddressAsync(pool1.Token0Address, _chainId, cancellationToken);
        if (token0 == null) return null;

        // Calculate prices in each pool
        var price1 = _priceService.CalculatePriceFromReserves(
            pool1.Reserve0, pool1.Reserve1,
            token0.Decimals, 18, true);

        var price2 = _priceService.CalculatePriceFromReserves(
            pool2.Reserve0, pool2.Reserve1,
            token0.Decimals, 18, true);

        if (price1 <= 0 || price2 <= 0)
            return null;

        // Determine buy/sell direction
        var (buyPool, sellPool, buyPrice, sellPrice) = price1 < price2
            ? (pool1, pool2, price1, price2)
            : (pool2, pool1, price2, price1);

        var spreadPercent = ((sellPrice - buyPrice) / buyPrice) * 100;

        // Minimum spread threshold (0.5%)
        if (spreadPercent < 0.5m)
            return null;

        // Calculate optimal amount
        var optimalResult = await CalculateOptimalAmountAsync(
            buyPool.Address, sellPool.Address, pool1.Token0Address, cancellationToken);

        if (optimalResult.IsFailure)
            return null;

        var (optimalInput, expectedProfit) = optimalResult.Value;
        if (optimalInput <= 0 || expectedProfit <= 0)
            return null;

        // Get USD value of profit
        var tokenPriceResult = await _priceService.GetTokenPriceUsdAsync(
            pool1.Token0Address, cancellationToken);

        var profitUsd = tokenPriceResult.IsSuccess
            ? expectedProfit * tokenPriceResult.Value.PriceUsd
            : expectedProfit; // Fallback to token units

        // Estimate gas cost
        var gasCostUsd = await EstimateSimpleArbitrageGas(cancellationToken);

        // Calculate confidence score
        var confidence = CalculateConfidenceScore(
            buyPool.Reserve0 + buyPool.Reserve1,
            sellPool.Reserve0 + sellPool.Reserve1,
            spreadPercent);

        var path = new List<ArbitrageLeg>
        {
            new()
            {
                PoolAddress = buyPool.Address,
                DexName = GetDexName(buyPool.Factory),
                TokenIn = "ETH",
                TokenOut = token0.Symbol,
                Rate = buyPrice,
                Liquidity = buyPool.Reserve0 + buyPool.Reserve1
            },
            new()
            {
                PoolAddress = sellPool.Address,
                DexName = GetDexName(sellPool.Factory),
                TokenIn = token0.Symbol,
                TokenOut = "ETH",
                Rate = sellPrice,
                Liquidity = sellPool.Reserve0 + sellPool.Reserve1
            }
        };

        return ArbitrageOpportunity.Create(
            pool1.Token0Address,
            token0.Symbol,
            path,
            buyPrice,
            sellPrice,
            optimalInput,
            profitUsd,
            gasCostUsd,
            confidence);
    }

    private async Task<ArbitrageOpportunity?> AnalyzeTriangularArbitrageAsync(
        Domain.Entities.Pool pool1,
        Domain.Entities.Pool pool2,
        Domain.Entities.Pool pool3,
        string baseToken,
        string intermediateToken,
        string thirdToken,
        CancellationToken cancellationToken)
    {
        // Calculate the product of all exchange rates
        // If product > 1, there's an arbitrage opportunity

        var rate1 = GetExchangeRate(pool1, baseToken, intermediateToken);
        var rate2 = GetExchangeRate(pool2, intermediateToken, thirdToken);
        var rate3 = GetExchangeRate(pool3, thirdToken, baseToken);

        if (rate1 <= 0 || rate2 <= 0 || rate3 <= 0)
            return null;

        var productRate = rate1 * rate2 * rate3;

        // Minimum 0.5% profit after considering slippage
        if (productRate <= 1.005m)
            return null;

        var spreadPercent = (productRate - 1) * 100;

        // Estimate optimal input (simplified)
        var minLiquidity = Math.Min(
            pool1.Reserve0 + pool1.Reserve1,
            Math.Min(pool2.Reserve0 + pool2.Reserve1, pool3.Reserve0 + pool3.Reserve1));
        var optimalInput = minLiquidity * 0.05m; // 5% of smallest pool

        // Get token info
        var baseTokenEntity = await _tokenRepository.GetByAddressAsync(baseToken, _chainId, cancellationToken);
        var expectedProfit = optimalInput * (productRate - 1);

        // Get USD value
        var priceResult = await _priceService.GetTokenPriceUsdAsync(baseToken, cancellationToken);
        var profitUsd = priceResult.IsSuccess
            ? expectedProfit * priceResult.Value.PriceUsd
            : expectedProfit;

        var gasCostUsd = await EstimateTriangularArbitrageGas(cancellationToken);

        var path = new List<ArbitrageLeg>
        {
            new() { PoolAddress = pool1.Address, DexName = GetDexName(pool1.Factory), TokenIn = baseToken, TokenOut = intermediateToken, Rate = rate1 },
            new() { PoolAddress = pool2.Address, DexName = GetDexName(pool2.Factory), TokenIn = intermediateToken, TokenOut = thirdToken, Rate = rate2 },
            new() { PoolAddress = pool3.Address, DexName = GetDexName(pool3.Factory), TokenIn = thirdToken, TokenOut = baseToken, Rate = rate3 }
        };

        return ArbitrageOpportunity.Create(
            baseToken,
            baseTokenEntity?.Symbol ?? "UNKNOWN",
            path,
            1m,
            productRate,
            optimalInput,
            profitUsd,
            gasCostUsd,
            CalculateConfidenceScore(minLiquidity, minLiquidity, spreadPercent));
    }

    private static string GetNormalizedPairKey(string token0, string token1)
    {
        var tokens = new[] { token0.ToLowerInvariant(), token1.ToLowerInvariant() };
        Array.Sort(tokens);
        return $"{tokens[0]}:{tokens[1]}";
    }

    private async Task<(decimal, decimal)> GetPoolPricesAsync(
        Domain.Entities.Pool buyPool,
        Domain.Entities.Pool sellPool,
        string tokenAddress,
        CancellationToken cancellationToken)
    {
        var token = await _tokenRepository.GetByAddressAsync(tokenAddress, _chainId, cancellationToken);
        var decimals = token?.Decimals ?? 18;

        var isBuyToken0 = buyPool.Token0Address.Equals(tokenAddress, StringComparison.OrdinalIgnoreCase);
        var buyPrice = _priceService.CalculatePriceFromReserves(
            buyPool.Reserve0, buyPool.Reserve1, decimals, 18, isBuyToken0);

        var isSellToken0 = sellPool.Token0Address.Equals(tokenAddress, StringComparison.OrdinalIgnoreCase);
        var sellPrice = _priceService.CalculatePriceFromReserves(
            sellPool.Reserve0, sellPool.Reserve1, decimals, 18, isSellToken0);

        return (buyPrice, sellPrice);
    }

    private static decimal GetReserveForToken(Domain.Entities.Pool pool, string tokenAddress)
    {
        return pool.Token0Address.Equals(tokenAddress, StringComparison.OrdinalIgnoreCase)
            ? pool.Reserve0
            : pool.Reserve1;
    }

    private static decimal CalculateAmountOut(decimal amountIn, Domain.Entities.Pool pool, string tokenInAddress)
    {
        var isToken0 = pool.Token0Address.Equals(tokenInAddress, StringComparison.OrdinalIgnoreCase);
        var reserveIn = isToken0 ? pool.Reserve0 : pool.Reserve1;
        var reserveOut = isToken0 ? pool.Reserve1 : pool.Reserve0;

        // Uniswap V2 formula with 0.3% fee
        var amountInWithFee = amountIn * 997;
        var numerator = amountInWithFee * reserveOut;
        var denominator = reserveIn * 1000 + amountInWithFee;

        return denominator > 0 ? numerator / denominator : 0;
    }

    private static decimal GetExchangeRate(Domain.Entities.Pool pool, string tokenIn, string tokenOut)
    {
        var isToken0In = pool.Token0Address.Equals(tokenIn, StringComparison.OrdinalIgnoreCase);
        var reserveIn = isToken0In ? pool.Reserve0 : pool.Reserve1;
        var reserveOut = isToken0In ? pool.Reserve1 : pool.Reserve0;

        return reserveIn > 0 ? reserveOut / reserveIn : 0;
    }

    private async Task<decimal> EstimateSimpleArbitrageGas(CancellationToken cancellationToken)
    {
        var gasUnits = BaseSwapGas * 2; // Two swaps
        return await CalculateGasCostUsd(gasUnits, cancellationToken);
    }

    private async Task<decimal> EstimateTriangularArbitrageGas(CancellationToken cancellationToken)
    {
        var gasUnits = BaseSwapGas * 3; // Three swaps
        return await CalculateGasCostUsd(gasUnits, cancellationToken);
    }

    private async Task<decimal> CalculateGasCostUsd(int gasUnits, CancellationToken cancellationToken)
    {
        var ethPriceResult = await _priceService.GetTokenPriceUsdAsync(WethAddress, cancellationToken);
        var ethPrice = ethPriceResult.IsSuccess ? ethPriceResult.Value.PriceUsd : 2000m;

        var gasCostEth = gasUnits * DefaultGasPriceGwei / 1_000_000_000m;
        return gasCostEth * ethPrice;
    }

    private static int CalculateConfidenceScore(decimal liquidity1, decimal liquidity2, decimal spreadPercent)
    {
        var score = 50;

        // Add points for liquidity
        var minLiquidity = Math.Min(liquidity1, liquidity2);
        if (minLiquidity > 1_000_000) score += 20;
        else if (minLiquidity > 100_000) score += 10;
        else if (minLiquidity < 10_000) score -= 20;

        // Adjust for spread (too high might indicate stale data)
        if (spreadPercent > 10) score -= 20;
        else if (spreadPercent > 5) score -= 10;
        else if (spreadPercent < 2) score += 10;

        return Math.Max(0, Math.Min(100, score));
    }

    private static string GetDexName(string factoryAddress)
    {
        return factoryAddress.ToLowerInvariant() switch
        {
            "0x5c69bee701ef814a2b6a3edd4b1652cb9cc5aa6f" => "Uniswap V2",
            "0xc0aee478e3658e2610c5f7a4a2e1777ce9e4f2ac" => "SushiSwap",
            "0x1f98431c8ad98523631ae4a59f267346ea31f984" => "Uniswap V3",
            _ => "Unknown DEX"
        };
    }
}
