using AnalyzerCore.Infrastructure.Caching;
using AnalyzerCore.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace AnalyzerCore.Infrastructure.Tests.Caching;

public class HybridCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly IMemoryCache _memoryCache;
    private readonly RedisOptions _redisOptions;
    private readonly InMemoryCacheService _memoryCacheService;
    private readonly RedisCacheService _redisCacheService;
    private readonly HybridCacheService _hybridCache;

    public HybridCacheServiceTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _redisOptions = new RedisOptions
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            InstanceName = "Test:",
            DefaultExpirationSeconds = 300
        };

        _memoryCacheService = new InMemoryCacheService(
            _memoryCache,
            NullLogger<InMemoryCacheService>.Instance);

        _redisCacheService = new RedisCacheService(
            _redisMock.Object,
            Options.Create(_redisOptions),
            NullLogger<RedisCacheService>.Instance);

        _hybridCache = new HybridCacheService(
            _redisCacheService,
            _memoryCacheService,
            Options.Create(_redisOptions),
            NullLogger<HybridCacheService>.Instance);
    }

    public record TestData(string Name, int Value);

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WhenInL1Cache_ShouldReturnFromMemory()
    {
        // Arrange
        var key = "test-key";
        var data = new TestData("L1", 1);
        await _memoryCacheService.SetAsync(key, data);

        // Act
        var result = await _hybridCache.GetAsync<TestData>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("L1");

        // Redis should not be called
        _databaseMock.Verify(x => x.StringGetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenOnlyInL2Cache_ShouldReturnFromRedisAndPopulateL1()
    {
        // Arrange
        var key = "test-key";
        var data = new TestData("L2", 2);
        var serialized = System.Text.Json.JsonSerializer.Serialize(data);

        _databaseMock.Setup(x => x.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(serialized);

        // Act
        var result = await _hybridCache.GetAsync<TestData>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("L2");

        // L1 should now have the value
        var l1Result = await _memoryCacheService.GetAsync<TestData>(key);
        l1Result.Should().NotBeNull();
        l1Result!.Name.Should().Be("L2");
    }

    [Fact]
    public async Task GetAsync_WhenNotInAnyCache_ShouldReturnNull()
    {
        // Arrange
        var key = "non-existent-key";
        _databaseMock.Setup(x => x.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _hybridCache.GetAsync<TestData>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenRedisUnavailable_ShouldFallbackToMemory()
    {
        // Arrange
        var key = "test-key";
        var data = new TestData("Memory", 1);
        await _memoryCacheService.SetAsync(key, data);

        _databaseMock.Setup(x => x.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        // Act
        var result = await _hybridCache.GetAsync<TestData>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Memory");
    }

    #endregion

    #region SetAsync Tests

    [Fact]
    public async Task SetAsync_ShouldSetInBothCaches()
    {
        // Arrange
        var key = "test-key";
        var data = new TestData("Test", 42);

        _databaseMock.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _hybridCache.SetAsync(key, data);

        // Assert
        var l1Result = await _memoryCacheService.GetAsync<TestData>(key);
        l1Result.Should().NotBeNull();

        _databaseMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WhenRedisUnavailable_ShouldStillSetInMemory()
    {
        // Arrange
        var key = "test-key";
        var data = new TestData("Test", 42);

        _databaseMock.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        // Act
        await _hybridCache.SetAsync(key, data);

        // Assert
        var l1Result = await _memoryCacheService.GetAsync<TestData>(key);
        l1Result.Should().NotBeNull();
        l1Result!.Name.Should().Be("Test");
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_ShouldRemoveFromBothCaches()
    {
        // Arrange
        var key = "test-key";
        var data = new TestData("Test", 42);

        await _memoryCacheService.SetAsync(key, data);

        _databaseMock.Setup(x => x.KeyDeleteAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _hybridCache.RemoveAsync(key);

        // Assert
        var l1Result = await _memoryCacheService.GetAsync<TestData>(key);
        l1Result.Should().BeNull();

        _databaseMock.Verify(x => x.KeyDeleteAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    #endregion
}
