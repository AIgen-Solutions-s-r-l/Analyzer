using AnalyzerCore.Application.Abstractions.Caching;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using AnalyzerCore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyzerCore.Infrastructure.Services;

/// <summary>
/// Service for price discovery and calculation from pool reserves.
/// </summary>
public sealed class PriceService : IPriceService
{
    private readonly IPoolRepository _poolRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PriceService> _logger;
    private readonly string _chainId;

    // Well-known stablecoin addresses (Ethereum mainnet)
    private static readonly HashSet<string> StablecoinAddresses = new(StringComparer.OrdinalIgnoreCase)
    {
        "0xdac17f958d2ee523a2206206994597c13d831ec7", // USDT
        "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48", // USDC
        "0x6b175474e89094c44da98b954eedeac495271d0f", // DAI
        "0x4fabb145d64652a948d72533023f6e7a623c7c53", // BUSD
        "0x0000000000085d4780b73119b644ae5ecd22b376"  // TUSD
    };

    // WETH address (Ethereum mainnet)
    private const string WethAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";

    private static readonly IReadOnlyList<string> SupportedQuoteCurrencies = new[]
    {
        "ETH", "WETH", "USDC", "USDT", "DAI", "USD"
    };

    public PriceService(
        IPoolRepository poolRepository,
        ITokenRepository tokenRepository,
        IPriceHistoryRepository priceHistoryRepository,
        ICacheService cacheService,
        IOptions<BlockchainOptions> blockchainOptions,
        ILogger<PriceService> logger)
    {
        _poolRepository = poolRepository;
        _tokenRepository = tokenRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _cacheService = cacheService;
        _chainId = blockchainOptions.Value.ChainId;
        _logger = logger;
    }

    public async Task<Result<TokenPrice>> GetTokenPriceAsync(
        string tokenAddress,
        string quoteCurrency = "ETH",
        CancellationToken cancellationToken = default)
    {
        tokenAddress = tokenAddress.ToLowerInvariant();
        var cacheKey = $"price:{tokenAddress}:{quoteCurrency}";

        // Try cache first
        var cached = await _cacheService.GetAsync<TokenPrice>(cacheKey);
        if (cached != null)
        {
            return Result.Success(cached);
        }

        // Get token info
        var token = await _tokenRepository.GetByAddressAsync(tokenAddress, _chainId, cancellationToken);
        if (token == null)
        {
            return Result.Failure<TokenPrice>(DomainErrors.Token.NotFound(tokenAddress));
        }

        // Find pools containing this token
        var pools = await _poolRepository.GetPoolsByTokenAsync(tokenAddress, _chainId, cancellationToken);
        if (!pools.Any())
        {
            return Result.Failure<TokenPrice>(
                new Error("Price.NoLiquidity", $"No liquidity pools found for token {tokenAddress}"));
        }

        // Find the best pool (highest liquidity with quote currency)
        var quoteTokenAddress = GetQuoteTokenAddress(quoteCurrency);
        var bestPool = pools
            .Where(p => quoteTokenAddress == null ||
                       p.Token0Address.Equals(quoteTokenAddress, StringComparison.OrdinalIgnoreCase) ||
                       p.Token1Address.Equals(quoteTokenAddress, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Reserve0 + p.Reserve1)
            .FirstOrDefault();

        if (bestPool == null)
        {
            // Try multi-hop through ETH
            var ethPrice = await GetMultiHopPriceAsync(tokenAddress, quoteCurrency, cancellationToken);
            if (ethPrice.IsSuccess)
            {
                return ethPrice;
            }

            return Result.Failure<TokenPrice>(
                new Error("Price.NoPool", $"No pool found for {token.Symbol}/{quoteCurrency}"));
        }

        // Calculate price
        var isToken0 = bestPool.Token0Address.Equals(tokenAddress, StringComparison.OrdinalIgnoreCase);
        var quoteToken = await _tokenRepository.GetByAddressAsync(
            isToken0 ? bestPool.Token1Address : bestPool.Token0Address,
            _chainId,
            cancellationToken);

        var price = CalculatePriceFromReserves(
            bestPool.Reserve0,
            bestPool.Reserve1,
            token.Decimals,
            quoteToken?.Decimals ?? 18,
            isToken0);

        // Calculate USD price if not already in USD
        decimal priceUsd = 0;
        if (IsStablecoin(quoteToken?.Address ?? ""))
        {
            priceUsd = price;
        }
        else
        {
            var usdPriceResult = await GetTokenPriceUsdAsync(
                quoteToken?.Address ?? WethAddress,
                cancellationToken);
            if (usdPriceResult.IsSuccess)
            {
                priceUsd = price * usdPriceResult.Value.PriceUsd;
            }
        }

        var tokenPrice = TokenPrice.Create(
            tokenAddress,
            quoteToken?.Address ?? "",
            quoteToken?.Symbol ?? quoteCurrency,
            price,
            priceUsd,
            bestPool.Address,
            bestPool.Reserve0 + bestPool.Reserve1);

        // Cache for 30 seconds
        await _cacheService.SetAsync(cacheKey, tokenPrice, TimeSpan.FromSeconds(30));

        _logger.LogDebug(
            "Calculated price for {Token}: {Price} {Quote} (${Usd})",
            token.Symbol, price, quoteCurrency, priceUsd);

        return Result.Success(tokenPrice);
    }

    public async Task<Result<TokenPrice>> GetTokenPriceUsdAsync(
        string tokenAddress,
        CancellationToken cancellationToken = default)
    {
        tokenAddress = tokenAddress.ToLowerInvariant();

        // If it's a stablecoin, return 1:1
        if (IsStablecoin(tokenAddress))
        {
            var stableToken = await _tokenRepository.GetByAddressAsync(tokenAddress, _chainId, cancellationToken);
            return Result.Success(TokenPrice.Create(
                tokenAddress,
                tokenAddress,
                stableToken?.Symbol ?? "USD",
                1m,
                1m,
                "",
                0));
        }

        // Try to find a direct USD pair
        var stablecoins = new[] { "USDC", "USDT", "DAI" };
        foreach (var stable in stablecoins)
        {
            var result = await GetTokenPriceAsync(tokenAddress, stable, cancellationToken);
            if (result.IsSuccess)
            {
                return Result.Success(TokenPrice.Create(
                    result.Value.TokenAddress,
                    result.Value.QuoteTokenAddress,
                    "USD",
                    result.Value.Price,
                    result.Value.Price, // Price is already in USD
                    result.Value.PoolAddress,
                    result.Value.Liquidity));
            }
        }

        // Try ETH route: Token -> ETH -> USD
        var ethPriceResult = await GetTokenPriceAsync(tokenAddress, "ETH", cancellationToken);
        if (ethPriceResult.IsFailure)
        {
            return Result.Failure<TokenPrice>(ethPriceResult.Error);
        }

        // Get ETH/USD price
        var ethUsdResult = await GetTokenPriceAsync(WethAddress, "USDC", cancellationToken);
        if (ethUsdResult.IsFailure)
        {
            return Result.Failure<TokenPrice>(
                new Error("Price.NoEthUsd", "Unable to determine ETH/USD price"));
        }

        var priceUsd = ethPriceResult.Value.Price * ethUsdResult.Value.Price;

        return Result.Success(TokenPrice.Create(
            tokenAddress,
            ethPriceResult.Value.QuoteTokenAddress,
            "USD",
            ethPriceResult.Value.Price,
            priceUsd,
            ethPriceResult.Value.PoolAddress,
            ethPriceResult.Value.Liquidity));
    }

    public async Task<Result<TwapResult>> GetTwapAsync(
        string tokenAddress,
        string quoteCurrency = "ETH",
        TimeSpan? period = null,
        CancellationToken cancellationToken = default)
    {
        tokenAddress = tokenAddress.ToLowerInvariant();
        period ??= TimeSpan.FromHours(1);

        var to = DateTime.UtcNow;
        var from = to - period.Value;

        var priceHistory = await _priceHistoryRepository.GetForTwapAsync(
            tokenAddress,
            quoteCurrency,
            from,
            to,
            cancellationToken);

        if (!priceHistory.Any())
        {
            return Result.Failure<TwapResult>(
                new Error("Twap.NoData", $"No price history found for TWAP calculation"));
        }

        // Calculate time-weighted average
        decimal totalWeightedPrice = 0;
        decimal totalWeight = 0;

        var sortedPrices = priceHistory.OrderBy(p => p.Timestamp).ToList();

        for (int i = 0; i < sortedPrices.Count - 1; i++)
        {
            var current = sortedPrices[i];
            var next = sortedPrices[i + 1];
            var duration = (decimal)(next.Timestamp - current.Timestamp).TotalSeconds;

            totalWeightedPrice += current.Price * duration;
            totalWeight += duration;
        }

        // Include the last price point
        if (sortedPrices.Count > 0)
        {
            var lastPrice = sortedPrices.Last();
            var remainingDuration = (decimal)(to - lastPrice.Timestamp).TotalSeconds;
            totalWeightedPrice += lastPrice.Price * remainingDuration;
            totalWeight += remainingDuration;
        }

        var twapPrice = totalWeight > 0 ? totalWeightedPrice / totalWeight : 0;
        var spotPrice = sortedPrices.LastOrDefault()?.Price ?? 0;
        var priceDeviation = spotPrice > 0 ? Math.Abs((twapPrice - spotPrice) / spotPrice) * 100 : 0;

        return Result.Success(new TwapResult
        {
            TokenAddress = tokenAddress,
            QuoteTokenSymbol = quoteCurrency,
            TwapPrice = twapPrice,
            SpotPrice = spotPrice,
            PriceDeviation = priceDeviation,
            Period = period.Value,
            DataPoints = priceHistory.Count,
            CalculatedAt = DateTime.UtcNow
        });
    }

    public async Task<Result<IReadOnlyList<TokenPrice>>> GetPriceHistoryAsync(
        string tokenAddress,
        string quoteCurrency = "ETH",
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        tokenAddress = tokenAddress.ToLowerInvariant();

        var history = await _priceHistoryRepository.GetByTokenAsync(
            tokenAddress,
            quoteCurrency,
            from,
            to,
            limit,
            cancellationToken);

        var prices = history.Select(h => new TokenPrice
        {
            TokenAddress = h.TokenAddress,
            QuoteTokenAddress = h.QuoteTokenAddress,
            QuoteTokenSymbol = h.QuoteTokenSymbol,
            Price = h.Price,
            PriceUsd = h.PriceUsd,
            PoolAddress = h.PoolAddress,
            Liquidity = h.Liquidity,
            Timestamp = h.Timestamp
        }).ToList();

        return Result.Success<IReadOnlyList<TokenPrice>>(prices);
    }

    public decimal CalculatePriceFromReserves(
        decimal reserve0,
        decimal reserve1,
        int decimals0,
        int decimals1,
        bool isToken0)
    {
        if (reserve0 == 0 || reserve1 == 0)
            return 0;

        // Normalize reserves to 18 decimals
        var normalizedReserve0 = reserve0 * (decimal)Math.Pow(10, 18 - decimals0);
        var normalizedReserve1 = reserve1 * (decimal)Math.Pow(10, 18 - decimals1);

        // Price = Quote/Base
        if (isToken0)
        {
            // Token0 is base, Token1 is quote
            return normalizedReserve1 / normalizedReserve0;
        }
        else
        {
            // Token1 is base, Token0 is quote
            return normalizedReserve0 / normalizedReserve1;
        }
    }

    public IReadOnlyList<string> GetSupportedQuoteCurrencies() => SupportedQuoteCurrencies;

    private async Task<Result<TokenPrice>> GetMultiHopPriceAsync(
        string tokenAddress,
        string quoteCurrency,
        CancellationToken cancellationToken)
    {
        // Try Token -> ETH -> QuoteCurrency
        var ethPriceResult = await GetTokenPriceAsync(tokenAddress, "ETH", cancellationToken);
        if (ethPriceResult.IsFailure)
        {
            return Result.Failure<TokenPrice>(ethPriceResult.Error);
        }

        if (quoteCurrency.Equals("ETH", StringComparison.OrdinalIgnoreCase) ||
            quoteCurrency.Equals("WETH", StringComparison.OrdinalIgnoreCase))
        {
            return ethPriceResult;
        }

        var quoteEthResult = await GetTokenPriceAsync(WethAddress, quoteCurrency, cancellationToken);
        if (quoteEthResult.IsFailure)
        {
            return Result.Failure<TokenPrice>(quoteEthResult.Error);
        }

        var finalPrice = ethPriceResult.Value.Price * quoteEthResult.Value.Price;

        return Result.Success(TokenPrice.Create(
            tokenAddress,
            quoteEthResult.Value.QuoteTokenAddress,
            quoteCurrency,
            finalPrice,
            ethPriceResult.Value.PriceUsd,
            ethPriceResult.Value.PoolAddress,
            ethPriceResult.Value.Liquidity));
    }

    private static string? GetQuoteTokenAddress(string quoteCurrency)
    {
        return quoteCurrency.ToUpperInvariant() switch
        {
            "ETH" or "WETH" => WethAddress,
            "USDC" => "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48",
            "USDT" => "0xdac17f958d2ee523a2206206994597c13d831ec7",
            "DAI" => "0x6b175474e89094c44da98b954eedeac495271d0f",
            _ => null
        };
    }

    private static bool IsStablecoin(string address)
    {
        return StablecoinAddresses.Contains(address.ToLowerInvariant());
    }
}
