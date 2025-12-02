using AnalyzerCore.Application.Abstractions.Caching;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using AnalyzerCore.Infrastructure.Configuration;
using AnalyzerCore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AnalyzerCore.Infrastructure.Tests.Services;

public class LiquidityAnalyticsServiceTests
{
    private readonly Mock<IPoolRepository> _poolRepositoryMock;
    private readonly Mock<ITokenRepository> _tokenRepositoryMock;
    private readonly Mock<IPriceService> _priceServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly IOptions<BlockchainOptions> _blockchainOptions;
    private readonly LiquidityAnalyticsService _sut;

    private const string WethAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
    private const string TestTokenAddress = "0x1234567890123456789012345678901234567890";
    private const string TestPoolAddress = "0xpool1234567890123456789012345678901234";

    public LiquidityAnalyticsServiceTests()
    {
        _poolRepositoryMock = new Mock<IPoolRepository>();
        _tokenRepositoryMock = new Mock<ITokenRepository>();
        _priceServiceMock = new Mock<IPriceService>();
        _cacheServiceMock = new Mock<ICacheService>();

        _blockchainOptions = Options.Create(new BlockchainOptions
        {
            ChainId = "1",
            Name = "Ethereum Mainnet",
            RpcUrl = "http://localhost:8545"
        });

        _sut = new LiquidityAnalyticsService(
            _poolRepositoryMock.Object,
            _tokenRepositoryMock.Object,
            _priceServiceMock.Object,
            _cacheServiceMock.Object,
            _blockchainOptions,
            NullLogger<LiquidityAnalyticsService>.Instance);
    }

    #region GetPoolMetricsAsync Tests

    [Fact]
    public async Task GetPoolMetricsAsync_WithCachedMetrics_ShouldReturnCached()
    {
        // Arrange
        var cachedMetrics = LiquidityMetrics.Create(
            TestPoolAddress, TestTokenAddress, "TEST", WethAddress, "WETH",
            1000m, 1m, 2000000m, 2000m, 50000m, 0.3m);

        _cacheServiceMock
            .Setup(x => x.GetAsync<LiquidityMetrics>(It.IsAny<string>()))
            .ReturnsAsync(cachedMetrics);

        // Act
        var result = await _sut.GetPoolMetricsAsync(TestPoolAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(cachedMetrics);
        _poolRepositoryMock.Verify(x => x.GetByAddressAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPoolMetricsAsync_WithMissingPool_ShouldReturnFailure()
    {
        // Arrange
        _cacheServiceMock
            .Setup(x => x.GetAsync<LiquidityMetrics>(It.IsAny<string>()))
            .ReturnsAsync((LiquidityMetrics?)null);

        _poolRepositoryMock
            .Setup(x => x.GetByAddressAsync(TestPoolAddress.ToLowerInvariant(), "", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pool?)null);

        // Act
        var result = await _sut.GetPoolMetricsAsync(TestPoolAddress);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Liquidity.PoolNotFound");
    }

    [Fact]
    public async Task GetPoolMetricsAsync_WithValidPool_ShouldCalculateMetrics()
    {
        // Arrange
        var pool = CreateTestPool(TestTokenAddress, WethAddress, 1000m, 1m, TestPoolAddress, "0xfactory");
        var token0 = CreateTestToken(TestTokenAddress, "TEST", 18);
        var token1 = CreateTestToken(WethAddress, "WETH", 18);

        _cacheServiceMock
            .Setup(x => x.GetAsync<LiquidityMetrics>(It.IsAny<string>()))
            .ReturnsAsync((LiquidityMetrics?)null);

        _poolRepositoryMock
            .Setup(x => x.GetByAddressAsync(TestPoolAddress.ToLowerInvariant(), "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pool);

        _tokenRepositoryMock
            .Setup(x => x.GetByAddressAsync(TestTokenAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token0);

        _tokenRepositoryMock
            .Setup(x => x.GetByAddressAsync(WethAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token1);

        var testPrice = TokenPrice.Create(TestTokenAddress, "", "USD", 2m, 2m, "", 0);
        var wethPrice = TokenPrice.Create(WethAddress, "", "USD", 2000m, 2000m, "", 0);

        _priceServiceMock
            .Setup(x => x.GetTokenPriceUsdAsync(TestTokenAddress.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Abstractions.Result.Success(testPrice));

        _priceServiceMock
            .Setup(x => x.GetTokenPriceUsdAsync(WethAddress.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Abstractions.Result.Success(wethPrice));

        // Act
        var result = await _sut.GetPoolMetricsAsync(TestPoolAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PoolAddress.Should().Be(pool.Address);
        result.Value.Token0Symbol.Should().Be("TEST");
        result.Value.Token1Symbol.Should().Be("WETH");
        result.Value.Reserve0.Should().Be(1000m);
        result.Value.Reserve1.Should().Be(1m);
        result.Value.TvlUsd.Should().BeGreaterThan(0);
    }

    #endregion

    #region GetTokenLiquiditySummaryAsync Tests

    [Fact]
    public async Task GetTokenLiquiditySummaryAsync_WithCachedSummary_ShouldReturnCached()
    {
        // Arrange
        var cachedSummary = new TokenLiquiditySummary
        {
            TokenAddress = TestTokenAddress,
            TokenSymbol = "TEST",
            TotalLiquidityUsd = 1000000m,
            PoolCount = 5,
            TopPools = new List<PoolLiquiditySummary>(),
            AverageLiquidityPerPool = 200000m,
            TotalVolume24hUsd = 50000m,
            Timestamp = DateTime.UtcNow
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<TokenLiquiditySummary>(It.IsAny<string>()))
            .ReturnsAsync(cachedSummary);

        // Act
        var result = await _sut.GetTokenLiquiditySummaryAsync(TestTokenAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(cachedSummary);
    }

    [Fact]
    public async Task GetTokenLiquiditySummaryAsync_WithMissingToken_ShouldReturnFailure()
    {
        // Arrange
        _cacheServiceMock
            .Setup(x => x.GetAsync<TokenLiquiditySummary>(It.IsAny<string>()))
            .ReturnsAsync((TokenLiquiditySummary?)null);

        _tokenRepositoryMock
            .Setup(x => x.GetByAddressAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Token?)null);

        // Act
        var result = await _sut.GetTokenLiquiditySummaryAsync(TestTokenAddress);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Liquidity.TokenNotFound");
    }

    [Fact]
    public async Task GetTokenLiquiditySummaryAsync_WithNoPools_ShouldReturnEmptySummary()
    {
        // Arrange
        var token = CreateTestToken(TestTokenAddress, "TEST", 18);

        _cacheServiceMock
            .Setup(x => x.GetAsync<TokenLiquiditySummary>(It.IsAny<string>()))
            .ReturnsAsync((TokenLiquiditySummary?)null);

        _tokenRepositoryMock
            .Setup(x => x.GetByAddressAsync(TestTokenAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _poolRepositoryMock
            .Setup(x => x.GetPoolsByTokenAsync(TestTokenAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Pool>());

        // Act
        var result = await _sut.GetTokenLiquiditySummaryAsync(TestTokenAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TokenSymbol.Should().Be("TEST");
        result.Value.PoolCount.Should().Be(0);
        result.Value.TotalLiquidityUsd.Should().Be(0);
    }

    #endregion

    #region GetTopPoolsByTvlAsync Tests

    [Fact]
    public async Task GetTopPoolsByTvlAsync_WithCachedResults_ShouldReturnCached()
    {
        // Arrange
        var cachedPools = new List<LiquidityMetrics>
        {
            LiquidityMetrics.Create(TestPoolAddress, TestTokenAddress, "TEST", WethAddress, "WETH", 1000m, 1m, 2000m, 2000m, 0m, 0.3m)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<LiquidityMetrics>>(It.IsAny<string>()))
            .ReturnsAsync(cachedPools);

        // Act
        var result = await _sut.GetTopPoolsByTvlAsync(limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTopPoolsByTvlAsync_WithNoPools_ShouldReturnEmptyList()
    {
        // Arrange
        _cacheServiceMock
            .Setup(x => x.GetAsync<List<LiquidityMetrics>>(It.IsAny<string>()))
            .ReturnsAsync((List<LiquidityMetrics>?)null);

        _poolRepositoryMock
            .Setup(x => x.GetAllByChainIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Pool>());

        // Act
        var result = await _sut.GetTopPoolsByTvlAsync(limit: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region CalculateImpermanentLossAsync Tests

    [Fact]
    public async Task CalculateImpermanentLossAsync_WithMissingPool_ShouldReturnFailure()
    {
        // Arrange
        _poolRepositoryMock
            .Setup(x => x.GetByAddressAsync(TestPoolAddress.ToLowerInvariant(), "", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pool?)null);

        // Act
        var result = await _sut.CalculateImpermanentLossAsync(TestPoolAddress, 1m, 1000m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Liquidity.PoolNotFound");
    }

    [Fact]
    public async Task CalculateImpermanentLossAsync_WithUnavailablePrices_ShouldReturnFailure()
    {
        // Arrange
        var pool = CreateTestPool(TestTokenAddress, WethAddress, 1000m, 1m, TestPoolAddress, "0xfactory");

        _poolRepositoryMock
            .Setup(x => x.GetByAddressAsync(TestPoolAddress.ToLowerInvariant(), "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pool);

        _priceServiceMock
            .Setup(x => x.GetTokenPriceUsdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Abstractions.Result.Failure<TokenPrice>(
                new Domain.Abstractions.Error("Price.Error", "Unable to fetch price")));

        // Act
        var result = await _sut.CalculateImpermanentLossAsync(TestPoolAddress, 1m, 1000m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Liquidity.PriceUnavailable");
    }

    [Fact]
    public async Task CalculateImpermanentLossAsync_WithValidData_ShouldCalculateIL()
    {
        // Arrange
        var pool = CreateTestPool(TestTokenAddress, WethAddress, 1000m, 1m, TestPoolAddress, "0xfactory");

        _poolRepositoryMock
            .Setup(x => x.GetByAddressAsync(TestPoolAddress.ToLowerInvariant(), "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pool);

        var testPrice = TokenPrice.Create(TestTokenAddress, "", "USD", 1m, 1m, "", 0);
        var wethPrice = TokenPrice.Create(WethAddress, "", "USD", 2000m, 2000m, "", 0);

        _priceServiceMock
            .Setup(x => x.GetTokenPriceUsdAsync(TestTokenAddress.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Abstractions.Result.Success(testPrice));

        _priceServiceMock
            .Setup(x => x.GetTokenPriceUsdAsync(WethAddress.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Abstractions.Result.Success(wethPrice));

        // Act
        var result = await _sut.CalculateImpermanentLossAsync(TestPoolAddress, 1m, 10000m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PoolAddress.Should().Be(TestPoolAddress.ToLowerInvariant());
        result.Value.InitialPriceRatio.Should().Be(1m);
    }

    #endregion

    #region GetHistoricalTvlAsync Tests

    [Fact]
    public async Task GetHistoricalTvlAsync_ShouldReturnEmptyList()
    {
        // This is a stub that returns empty (requires event indexing)
        // Act
        var result = await _sut.GetHistoricalTvlAsync(TestPoolAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region GetLiquidityConcentrationAsync Tests

    [Fact]
    public async Task GetLiquidityConcentrationAsync_WithNoLiquidity_ShouldReturnZeroConcentration()
    {
        // Arrange
        var token = CreateTestToken(TestTokenAddress, "TEST", 18);

        _cacheServiceMock
            .Setup(x => x.GetAsync<TokenLiquiditySummary>(It.IsAny<string>()))
            .ReturnsAsync((TokenLiquiditySummary?)null);

        _tokenRepositoryMock
            .Setup(x => x.GetByAddressAsync(TestTokenAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        _poolRepositoryMock
            .Setup(x => x.GetPoolsByTokenAsync(TestTokenAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Pool>());

        // Act
        var result = await _sut.GetLiquidityConcentrationAsync(TestTokenAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PoolCount.Should().Be(0);
        result.Value.ConcentrationLevel.Should().Be("No Liquidity");
        result.Value.HhiIndex.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private static Pool CreateTestPool(string token0Address, string token1Address, decimal reserve0, decimal reserve1, string poolAddress, string factoryAddress)
    {
        var token0 = CreateTestToken(token0Address, "TOKEN0", 18);
        var token1 = CreateTestToken(token1Address, "TOKEN1", 18);

        var poolResult = Pool.Create(poolAddress, factoryAddress, token0, token1, PoolType.UniswapV2);
        var pool = poolResult.Value;
        pool.UpdateReserves(reserve0, reserve1);

        return pool;
    }

    private static Token CreateTestToken(string address, string symbol, int decimals)
    {
        var tokenResult = Token.Create(address, "1", symbol, $"{symbol} Token", decimals);
        return tokenResult.Value;
    }

    #endregion
}
