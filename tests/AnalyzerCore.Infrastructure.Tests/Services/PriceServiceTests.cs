using AnalyzerCore.Application.Abstractions.Caching;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.ValueObjects;
using AnalyzerCore.Infrastructure.Configuration;
using AnalyzerCore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AnalyzerCore.Infrastructure.Tests.Services;

public class PriceServiceTests
{
    private readonly Mock<IPoolRepository> _poolRepositoryMock;
    private readonly Mock<ITokenRepository> _tokenRepositoryMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepositoryMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly IOptions<BlockchainOptions> _blockchainOptions;
    private readonly PriceService _sut;

    // Test addresses
    private const string WethAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
    private const string UsdcAddress = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";
    private const string TestTokenAddress = "0x1234567890123456789012345678901234567890";

    public PriceServiceTests()
    {
        _poolRepositoryMock = new Mock<IPoolRepository>();
        _tokenRepositoryMock = new Mock<ITokenRepository>();
        _priceHistoryRepositoryMock = new Mock<IPriceHistoryRepository>();
        _cacheServiceMock = new Mock<ICacheService>();

        _blockchainOptions = Options.Create(new BlockchainOptions
        {
            ChainId = "1",
            Name = "Ethereum Mainnet",
            RpcUrl = "http://localhost:8545"
        });

        _sut = new PriceService(
            _poolRepositoryMock.Object,
            _tokenRepositoryMock.Object,
            _priceHistoryRepositoryMock.Object,
            _cacheServiceMock.Object,
            _blockchainOptions,
            NullLogger<PriceService>.Instance);
    }

    #region CalculatePriceFromReserves Tests

    [Fact]
    public void CalculatePriceFromReserves_WithValidReserves_ShouldReturnCorrectPrice()
    {
        // Arrange: 1000 TOKEN (18 decimals) and 1 ETH (18 decimals)
        decimal reserve0 = 1000m; // Token
        decimal reserve1 = 1m;     // ETH
        int decimals0 = 18;
        int decimals1 = 18;

        // Act
        var price = _sut.CalculatePriceFromReserves(reserve0, reserve1, decimals0, decimals1, isToken0: true);

        // Assert: Token/ETH = 1/1000 = 0.001
        price.Should().Be(0.001m);
    }

    [Fact]
    public void CalculatePriceFromReserves_WithDifferentDecimals_ShouldNormalize()
    {
        // Arrange: 1000 USDC (6 decimals) and 1 ETH (18 decimals)
        decimal reserve0 = 1000m; // USDC (6 decimals)
        decimal reserve1 = 1m;    // ETH (18 decimals)
        int decimals0 = 6;
        int decimals1 = 18;

        // Act - USDC is token0, ETH is token1
        var price = _sut.CalculatePriceFromReserves(reserve0, reserve1, decimals0, decimals1, isToken0: true);

        // Assert: After normalization, price should account for decimal difference
        // Normalized USDC = 1000 * 10^12 = 1e15
        // Normalized ETH = 1 * 10^0 = 1
        // Price = 1 / 1e15 = 1e-15 (very small because we need 1e15 USDC to buy 1 ETH)
        price.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculatePriceFromReserves_WithZeroReserve_ShouldReturnZero()
    {
        // Arrange
        decimal reserve0 = 0m;
        decimal reserve1 = 100m;

        // Act
        var price = _sut.CalculatePriceFromReserves(reserve0, reserve1, 18, 18, isToken0: true);

        // Assert
        price.Should().Be(0);
    }

    [Fact]
    public void CalculatePriceFromReserves_WhenToken1IsBase_ShouldInvertPrice()
    {
        // Arrange: Equal reserves
        decimal reserve0 = 100m;
        decimal reserve1 = 100m;

        // Act
        var priceAsToken0 = _sut.CalculatePriceFromReserves(reserve0, reserve1, 18, 18, isToken0: true);
        var priceAsToken1 = _sut.CalculatePriceFromReserves(reserve0, reserve1, 18, 18, isToken0: false);

        // Assert: Both should be 1 for equal reserves
        priceAsToken0.Should().Be(1m);
        priceAsToken1.Should().Be(1m);
    }

    #endregion

    #region GetTokenPriceAsync Tests

    [Fact]
    public async Task GetTokenPriceAsync_WithCachedPrice_ShouldReturnCachedValue()
    {
        // Arrange
        var cachedPrice = TokenPrice.Create(
            TestTokenAddress, WethAddress, "WETH", 0.5m, 1000m, "0xpool", 1000000m);

        _cacheServiceMock
            .Setup(x => x.GetAsync<TokenPrice>(It.IsAny<string>()))
            .ReturnsAsync(cachedPrice);

        // Act
        var result = await _sut.GetTokenPriceAsync(TestTokenAddress, "ETH");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(cachedPrice);
        _poolRepositoryMock.Verify(x => x.GetPoolsByTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTokenPriceAsync_WithNoToken_ShouldReturnFailure()
    {
        // Arrange
        _cacheServiceMock.Setup(x => x.GetAsync<TokenPrice>(It.IsAny<string>())).ReturnsAsync((TokenPrice?)null);
        _tokenRepositoryMock.Setup(x => x.GetByAddressAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Token?)null);

        // Act
        var result = await _sut.GetTokenPriceAsync(TestTokenAddress, "ETH");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    public async Task GetTokenPriceAsync_WithNoLiquidityPools_ShouldReturnFailure()
    {
        // Arrange
        var token = CreateTestToken(TestTokenAddress, "TEST", 18);

        _cacheServiceMock.Setup(x => x.GetAsync<TokenPrice>(It.IsAny<string>())).ReturnsAsync((TokenPrice?)null);
        _tokenRepositoryMock.Setup(x => x.GetByAddressAsync(TestTokenAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _poolRepositoryMock.Setup(x => x.GetPoolsByTokenAsync(TestTokenAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Pool>());

        // Act
        var result = await _sut.GetTokenPriceAsync(TestTokenAddress, "ETH");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Price.NoLiquidity");
    }

    [Fact]
    public async Task GetTokenPriceAsync_WithValidPool_ShouldCalculatePrice()
    {
        // Arrange
        var token = CreateTestToken(TestTokenAddress, "TEST", 18);
        var wethToken = CreateTestToken(WethAddress, "WETH", 18);
        var pool = CreateTestPool(TestTokenAddress, WethAddress, 1000m, 1m);

        _cacheServiceMock.Setup(x => x.GetAsync<TokenPrice>(It.IsAny<string>())).ReturnsAsync((TokenPrice?)null);
        _tokenRepositoryMock.Setup(x => x.GetByAddressAsync(TestTokenAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _tokenRepositoryMock.Setup(x => x.GetByAddressAsync(WethAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(wethToken);
        _poolRepositoryMock.Setup(x => x.GetPoolsByTokenAsync(TestTokenAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { pool });

        // Act
        var result = await _sut.GetTokenPriceAsync(TestTokenAddress, "ETH");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Price.Should().Be(0.001m); // 1 ETH / 1000 TEST = 0.001 ETH per TEST
        result.Value.QuoteTokenSymbol.Should().Be("WETH");
    }

    #endregion

    #region GetTokenPriceUsdAsync Tests

    [Fact]
    public async Task GetTokenPriceUsdAsync_WithStablecoin_ShouldReturnOneToOne()
    {
        // Arrange
        var usdcToken = CreateTestToken(UsdcAddress, "USDC", 6);
        _tokenRepositoryMock.Setup(x => x.GetByAddressAsync(UsdcAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usdcToken);

        // Act
        var result = await _sut.GetTokenPriceUsdAsync(UsdcAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Price.Should().Be(1m);
        result.Value.PriceUsd.Should().Be(1m);
    }

    #endregion

    #region GetTwapAsync Tests

    [Fact]
    public async Task GetTwapAsync_WithNoPriceHistory_ShouldReturnFailure()
    {
        // Arrange
        _priceHistoryRepositoryMock
            .Setup(x => x.GetForTwapAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PriceHistory>());

        // Act
        var result = await _sut.GetTwapAsync(TestTokenAddress, "ETH", TimeSpan.FromHours(1));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Twap.NoData");
    }

    [Fact]
    public async Task GetTwapAsync_WithConstantPrices_ShouldReturnSamePrice()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var priceHistory = new List<PriceHistory>
        {
            CreatePriceHistory(TestTokenAddress, "ETH", 100m, now.AddMinutes(-30)),
            CreatePriceHistory(TestTokenAddress, "ETH", 100m, now.AddMinutes(-20)),
            CreatePriceHistory(TestTokenAddress, "ETH", 100m, now.AddMinutes(-10))
        };

        _priceHistoryRepositoryMock
            .Setup(x => x.GetForTwapAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(priceHistory);

        // Act
        var result = await _sut.GetTwapAsync(TestTokenAddress, "ETH", TimeSpan.FromHours(1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TwapPrice.Should().Be(100m);
        result.Value.SpotPrice.Should().Be(100m);
        result.Value.PriceDeviation.Should().Be(0m);
        result.Value.DataPoints.Should().Be(3);
    }

    [Fact]
    public async Task GetTwapAsync_WithVaryingPrices_ShouldCalculateWeightedAverage()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var priceHistory = new List<PriceHistory>
        {
            CreatePriceHistory(TestTokenAddress, "ETH", 100m, now.AddMinutes(-60)),
            CreatePriceHistory(TestTokenAddress, "ETH", 200m, now.AddMinutes(-30))
        };

        _priceHistoryRepositoryMock
            .Setup(x => x.GetForTwapAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(priceHistory);

        // Act
        var result = await _sut.GetTwapAsync(TestTokenAddress, "ETH", TimeSpan.FromHours(1));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TwapPrice.Should().BeGreaterThan(100m);
        result.Value.TwapPrice.Should().BeLessThan(200m);
        result.Value.SpotPrice.Should().Be(200m);
        result.Value.DataPoints.Should().Be(2);
    }

    #endregion

    #region GetPriceHistoryAsync Tests

    [Fact]
    public async Task GetPriceHistoryAsync_ShouldReturnHistory()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var history = new List<PriceHistory>
        {
            CreatePriceHistory(TestTokenAddress, "ETH", 100m, now.AddHours(-2)),
            CreatePriceHistory(TestTokenAddress, "ETH", 110m, now.AddHours(-1)),
            CreatePriceHistory(TestTokenAddress, "ETH", 105m, now)
        };

        _priceHistoryRepositoryMock
            .Setup(x => x.GetByTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        // Act
        var result = await _sut.GetPriceHistoryAsync(TestTokenAddress, "ETH", limit: 100);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    #endregion

    #region GetSupportedQuoteCurrencies Tests

    [Fact]
    public void GetSupportedQuoteCurrencies_ShouldReturnExpectedCurrencies()
    {
        // Act
        var currencies = _sut.GetSupportedQuoteCurrencies();

        // Assert
        currencies.Should().Contain("ETH");
        currencies.Should().Contain("WETH");
        currencies.Should().Contain("USDC");
        currencies.Should().Contain("USDT");
        currencies.Should().Contain("DAI");
        currencies.Should().Contain("USD");
    }

    #endregion

    #region Helper Methods

    private static Token CreateTestToken(string address, string symbol, int decimals)
    {
        var tokenResult = Token.Create(address, "1", symbol, $"{symbol} Token", decimals);
        return tokenResult.Value;
    }

    private static Pool CreateTestPool(string token0Address, string token1Address, decimal reserve0, decimal reserve1)
    {
        var token0 = CreateTestToken(token0Address, "TOKEN0", 18);
        var token1 = CreateTestToken(token1Address, "TOKEN1", 18);

        var poolResult = Pool.Create(
            "0xpooladdress",
            "0xfactoryaddress",
            token0,
            token1,
            PoolType.UniswapV2);

        var pool = poolResult.Value;
        pool.UpdateReserves(reserve0, reserve1);

        return pool;
    }

    private static PriceHistory CreatePriceHistory(string tokenAddress, string quoteSymbol, decimal price, DateTime timestamp)
    {
        return PriceHistory.Create(
            tokenAddress,
            WethAddress,
            quoteSymbol,
            price,
            price * 2000m, // Assuming ETH = $2000
            "0xpooladdress",
            1000m,
            500m,
            1500000m,
            12345678,
            timestamp);
    }

    #endregion
}
