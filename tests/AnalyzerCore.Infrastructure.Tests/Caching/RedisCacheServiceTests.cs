using AnalyzerCore.Infrastructure.Caching;
using AnalyzerCore.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace AnalyzerCore.Infrastructure.Tests.Caching;

public class RedisCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly RedisOptions _options;
    private readonly RedisCacheService _cacheService;

    public RedisCacheServiceTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _options = new RedisOptions
        {
            Enabled = true,
            ConnectionString = "localhost:6379",
            InstanceName = "Test:",
            DefaultExpirationSeconds = 300
        };

        _cacheService = new RedisCacheService(
            _redisMock.Object,
            Options.Create(_options),
            NullLogger<RedisCacheService>.Instance);
    }

    public record TestData(string Name, int Value);

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnDeserializedValue()
    {
        // Arrange
        var key = "test-key";
        var expectedData = new TestData("Test", 42);
        var serialized = System.Text.Json.JsonSerializer.Serialize(expectedData);

        _databaseMock.Setup(x => x.StringGetAsync(
                It.Is<RedisKey>(k => k == $"{_options.InstanceName}{key}"),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(serialized);

        // Act
        var result = await _cacheService.GetAsync<TestData>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(expectedData.Name);
        result.Value.Should().Be(expectedData.Value);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var key = "non-existent-key";
        _databaseMock.Setup(x => x.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _cacheService.GetAsync<TestData>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenRedisThrows_ShouldReturnNull()
    {
        // Arrange
        var key = "error-key";
        _databaseMock.Setup(x => x.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Connection failed"));

        // Act
        var result = await _cacheService.GetAsync<TestData>(key);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SetAsync Tests

    [Fact]
    public async Task SetAsync_ShouldSerializeAndStoreValue()
    {
        // Arrange
        var key = "test-key";
        var data = new TestData("Test", 42);
        var expiration = TimeSpan.FromMinutes(10);

        _databaseMock.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _cacheService.SetAsync(key, data, expiration);

        // Assert
        _databaseMock.Verify(x => x.StringSetAsync(
            It.Is<RedisKey>(k => k == $"{_options.InstanceName}{key}"),
            It.Is<RedisValue>(v => v.ToString().Contains("Test")),
            It.Is<TimeSpan?>(t => t == expiration),
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithoutExpiration_ShouldUseDefaultExpiration()
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
        await _cacheService.SetAsync(key, data);

        // Assert
        _databaseMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.Is<TimeSpan?>(t => t == TimeSpan.FromSeconds(_options.DefaultExpirationSeconds)),
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_ShouldDeleteKey()
    {
        // Arrange
        var key = "test-key";

        _databaseMock.Setup(x => x.KeyDeleteAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _databaseMock.Verify(x => x.KeyDeleteAsync(
            It.Is<RedisKey>(k => k == $"{_options.InstanceName}{key}"),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    #endregion

    #region GetOrSetAsync Tests

    [Fact]
    public async Task GetOrSetAsync_WhenCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var key = "test-key";
        var cachedData = new TestData("Cached", 100);
        var serialized = System.Text.Json.JsonSerializer.Serialize(cachedData);
        var factoryCalled = false;

        _databaseMock.Setup(x => x.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(serialized);

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            async ct =>
            {
                factoryCalled = true;
                return new TestData("Fresh", 200);
            });

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Cached");
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheMiss_ShouldCallFactoryAndCache()
    {
        // Arrange
        var key = "test-key";
        var freshData = new TestData("Fresh", 200);

        _databaseMock.Setup(x => x.StringGetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        _databaseMock.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            async ct => freshData);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Fresh");
        result.Value.Should().Be(200);

        _databaseMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    #endregion
}
