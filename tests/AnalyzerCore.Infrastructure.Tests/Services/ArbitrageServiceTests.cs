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

public class ArbitrageServiceTests
{
    private readonly Mock<IPoolRepository> _poolRepositoryMock;
    private readonly Mock<ITokenRepository> _tokenRepositoryMock;
    private readonly Mock<IPriceService> _priceServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly IOptions<BlockchainOptions> _blockchainOptions;
    private readonly ArbitrageService _sut;

    private const string WethAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
    private const string TestTokenAddress = "0x1234567890123456789012345678901234567890";

    public ArbitrageServiceTests()
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

        _sut = new ArbitrageService(
            _poolRepositoryMock.Object,
            _tokenRepositoryMock.Object,
            _priceServiceMock.Object,
            _cacheServiceMock.Object,
            _blockchainOptions,
            NullLogger<ArbitrageService>.Instance);
    }

    #region ScanForOpportunitiesAsync Tests

    [Fact]
    public async Task ScanForOpportunitiesAsync_WithCachedResults_ShouldReturnCached()
    {
        // Arrange
        var cachedOpportunities = new List<ArbitrageOpportunity>
        {
            CreateTestOpportunity(TestTokenAddress, 100m)
        };

        _cacheServiceMock
            .Setup(x => x.GetAsync<List<ArbitrageOpportunity>>(It.IsAny<string>()))
            .ReturnsAsync(cachedOpportunities);

        // Act
        var result = await _sut.ScanForOpportunitiesAsync(minProfitUsd: 10m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        _poolRepositoryMock.Verify(x => x.GetAllByChainIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScanForOpportunitiesAsync_WithNoPools_ShouldReturnEmptyList()
    {
        // Arrange
        _cacheServiceMock
            .Setup(x => x.GetAsync<List<ArbitrageOpportunity>>(It.IsAny<string>()))
            .ReturnsAsync((List<ArbitrageOpportunity>?)null);

        _poolRepositoryMock
            .Setup(x => x.GetAllByChainIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Pool>());

        // Act
        var result = await _sut.ScanForOpportunitiesAsync(minProfitUsd: 10m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanForOpportunitiesAsync_WithSinglePoolPerPair_ShouldReturnEmptyList()
    {
        // Arrange
        _cacheServiceMock
            .Setup(x => x.GetAsync<List<ArbitrageOpportunity>>(It.IsAny<string>()))
            .ReturnsAsync((List<ArbitrageOpportunity>?)null);

        var pool = CreateTestPool(TestTokenAddress, WethAddress, 1000m, 1m, "0xpool1", "0xfactory1");

        _poolRepositoryMock
            .Setup(x => x.GetAllByChainIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { pool });

        // Act
        var result = await _sut.ScanForOpportunitiesAsync(minProfitUsd: 10m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region FindOpportunitiesForTokenAsync Tests

    [Fact]
    public async Task FindOpportunitiesForTokenAsync_WithSinglePool_ShouldReturnEmptyList()
    {
        // Arrange
        var pool = CreateTestPool(TestTokenAddress, WethAddress, 1000m, 1m, "0xpool1", "0xfactory1");

        _poolRepositoryMock
            .Setup(x => x.GetPoolsByTokenAsync(TestTokenAddress.ToLowerInvariant(), "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { pool });

        // Act
        var result = await _sut.FindOpportunitiesForTokenAsync(TestTokenAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task FindOpportunitiesForTokenAsync_WithNoPools_ShouldReturnEmptyList()
    {
        // Arrange
        _poolRepositoryMock
            .Setup(x => x.GetPoolsByTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Pool>());

        // Act
        var result = await _sut.FindOpportunitiesForTokenAsync(TestTokenAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region FindTriangularOpportunitiesAsync Tests

    [Fact]
    public async Task FindTriangularOpportunitiesAsync_WithNoBasePools_ShouldReturnEmptyList()
    {
        // Arrange
        _poolRepositoryMock
            .Setup(x => x.GetPoolsByTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Pool>());

        // Act
        var result = await _sut.FindTriangularOpportunitiesAsync(WethAddress);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region CalculateOptimalAmountAsync Tests

    [Fact]
    public async Task CalculateOptimalAmountAsync_WithMissingPool_ShouldReturnFailure()
    {
        // Arrange
        _poolRepositoryMock
            .Setup(x => x.GetByAddressAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pool?)null);

        // Act
        var result = await _sut.CalculateOptimalAmountAsync("0xbuy", "0xsell", TestTokenAddress);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Arbitrage.PoolNotFound");
    }

    #endregion

    #region EstimateGasCostAsync Tests

    [Fact]
    public async Task EstimateGasCostAsync_WithValidEthPrice_ShouldCalculateCost()
    {
        // Arrange
        var opportunity = CreateTestOpportunity(TestTokenAddress, 100m);
        var ethPrice = TokenPrice.Create(WethAddress, "", "USD", 2000m, 2000m, "", 0);

        _priceServiceMock
            .Setup(x => x.GetTokenPriceUsdAsync(WethAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Abstractions.Result.Success(ethPrice));

        // Act
        var result = await _sut.EstimateGasCostAsync(opportunity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EstimateGasCostAsync_WithNoEthPrice_ShouldReturnDefaultEstimate()
    {
        // Arrange
        var opportunity = CreateTestOpportunity(TestTokenAddress, 100m);

        _priceServiceMock
            .Setup(x => x.GetTokenPriceUsdAsync(WethAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Domain.Abstractions.Result.Failure<TokenPrice>(
                new Domain.Abstractions.Error("Price.Error", "Unable to fetch price")));

        // Act
        var result = await _sut.EstimateGasCostAsync(opportunity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(50m); // Default estimate
    }

    #endregion

    #region GetHistoricalOpportunitiesAsync Tests

    [Fact]
    public async Task GetHistoricalOpportunitiesAsync_ShouldReturnEmptyList()
    {
        // This is a stub implementation that returns empty
        // Act
        var result = await _sut.GetHistoricalOpportunitiesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
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

    private static ArbitrageOpportunity CreateTestOpportunity(string tokenAddress, decimal profitUsd)
    {
        var path = new List<ArbitrageLeg>
        {
            new()
            {
                PoolAddress = "0xpool1",
                DexName = "Uniswap V2",
                TokenIn = "ETH",
                TokenOut = "TOKEN",
                Rate = 1000m,
                Liquidity = 1000000m
            },
            new()
            {
                PoolAddress = "0xpool2",
                DexName = "SushiSwap",
                TokenIn = "TOKEN",
                TokenOut = "ETH",
                Rate = 0.00102m,
                Liquidity = 900000m
            }
        };

        return ArbitrageOpportunity.Create(
            tokenAddress,
            "TOKEN",
            path,
            buyPrice: 1000m,
            sellPrice: 1020m,
            optimalAmount: 1m,
            grossProfitUsd: profitUsd + 10m,
            estimatedGasCostUsd: 10m,
            confidenceScore: 75);
    }

    #endregion
}
