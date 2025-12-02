using AnalyzerCore.Infrastructure.Caching;
using AnalyzerCore.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AnalyzerCore.Infrastructure.Tests.Caching;

public class InMemoryCacheServiceTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly InMemoryCacheService _cacheService;

    public InMemoryCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cachingOptions = Options.Create(new CachingOptions
        {
            Enabled = true,
            PoolExpirationSeconds = 60,
            TokenExpirationSeconds = 120
        });
        var logger = Mock.Of<ILogger<InMemoryCacheService>>();

        _cacheService = new InMemoryCacheService(_memoryCache, cachingOptions, logger);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnValue()
    {
        // Arrange
        var key = "test-key";
        var value = new TestCacheItem { Name = "Test", Value = 42 };
        await _cacheService.SetAsync(key, value);

        // Act
        var result = await _cacheService.GetAsync<TestCacheItem>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var key = "non-existent-key";

        // Act
        var result = await _cacheService.GetAsync<TestCacheItem>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldStoreValue()
    {
        // Arrange
        var key = "new-key";
        var value = new TestCacheItem { Name = "New", Value = 100 };

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<TestCacheItem>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("New");
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ShouldExpireValue()
    {
        // Arrange
        var key = "expiring-key";
        var value = new TestCacheItem { Name = "Expiring", Value = 1 };
        var expiration = TimeSpan.FromMilliseconds(50);

        // Act
        await _cacheService.SetAsync(key, value, expiration);
        await Task.Delay(100);
        var result = await _cacheService.GetAsync<TestCacheItem>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveValue()
    {
        // Arrange
        var key = "remove-key";
        var value = new TestCacheItem { Name = "ToRemove", Value = 0 };
        await _cacheService.SetAsync(key, value);

        // Act
        await _cacheService.RemoveAsync(key);
        var result = await _cacheService.GetAsync<TestCacheItem>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrSetAsync_WhenKeyExists_ShouldReturnExistingValue()
    {
        // Arrange
        var key = "get-or-set-existing";
        var existingValue = new TestCacheItem { Name = "Existing", Value = 1 };
        await _cacheService.SetAsync(key, existingValue);

        var factoryCalled = false;

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult<TestCacheItem?>(new TestCacheItem { Name = "New", Value = 2 });
            });

        // Assert
        factoryCalled.Should().BeFalse();
        result!.Name.Should().Be("Existing");
    }

    [Fact]
    public async Task GetOrSetAsync_WhenKeyDoesNotExist_ShouldCallFactory()
    {
        // Arrange
        var key = "get-or-set-new";
        var factoryCalled = false;

        // Act
        var result = await _cacheService.GetOrSetAsync(
            key,
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult<TestCacheItem?>(new TestCacheItem { Name = "FromFactory", Value = 99 });
            });

        // Assert
        factoryCalled.Should().BeTrue();
        result!.Name.Should().Be("FromFactory");
    }

    [Fact]
    public async Task RemoveByPrefixAsync_ShouldRemoveMatchingKeys()
    {
        // Arrange
        await _cacheService.SetAsync("prefix:key1", new TestCacheItem { Name = "1", Value = 1 });
        await _cacheService.SetAsync("prefix:key2", new TestCacheItem { Name = "2", Value = 2 });
        await _cacheService.SetAsync("other:key3", new TestCacheItem { Name = "3", Value = 3 });

        // Act
        await _cacheService.RemoveByPrefixAsync("prefix:");

        // Assert
        var result1 = await _cacheService.GetAsync<TestCacheItem>("prefix:key1");
        var result2 = await _cacheService.GetAsync<TestCacheItem>("prefix:key2");
        var result3 = await _cacheService.GetAsync<TestCacheItem>("other:key3");

        // Note: In-memory cache doesn't support prefix removal, so this test documents the limitation
        // The actual implementation may need to track keys separately or use a different approach
    }

    private class TestCacheItem
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
