using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Infrastructure.Persistence;
using AnalyzerCore.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnalyzerCore.Infrastructure.Tests.Repositories;

public class SwapEventRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SwapEventRepository _repository;

    public SwapEventRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var logger = Mock.Of<ILogger<SwapEventRepository>>();
        _repository = new SwapEventRepository(_context, logger);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddSwapEvent()
    {
        // Arrange
        var swapEvent = CreateSwapEvent("0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852", 100m);

        // Act
        await _repository.AddAsync(swapEvent);

        // Assert
        var count = await _context.SwapEvents.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task AddRangeAsync_ShouldAddMultipleEvents()
    {
        // Arrange
        var events = new[]
        {
            CreateSwapEvent("0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852", 100m, "0x1"),
            CreateSwapEvent("0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852", 200m, "0x2"),
            CreateSwapEvent("0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852", 300m, "0x3")
        };

        // Act
        await _repository.AddRangeAsync(events);

        // Assert
        var count = await _context.SwapEvents.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetPoolVolumeAsync_ShouldReturnTotalVolume()
    {
        // Arrange
        var poolAddress = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852";
        var now = DateTime.UtcNow;

        await _repository.AddRangeAsync(new[]
        {
            CreateSwapEvent(poolAddress, 100m, "0x1", now.AddHours(-1)),
            CreateSwapEvent(poolAddress, 200m, "0x2", now.AddHours(-2)),
            CreateSwapEvent(poolAddress, 50m, "0x3", now.AddHours(-3))
        });

        // Act
        var volume = await _repository.GetPoolVolumeAsync(
            poolAddress,
            now.AddDays(-1),
            now);

        // Assert
        volume.Should().Be(350m);
    }

    [Fact]
    public async Task GetPoolVolumeAsync_ShouldFilterByTimeRange()
    {
        // Arrange
        var poolAddress = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852";
        var now = DateTime.UtcNow;

        await _repository.AddRangeAsync(new[]
        {
            CreateSwapEvent(poolAddress, 100m, "0x1", now.AddHours(-1)),
            CreateSwapEvent(poolAddress, 200m, "0x2", now.AddDays(-2)), // Outside range
            CreateSwapEvent(poolAddress, 50m, "0x3", now.AddHours(-3))
        });

        // Act
        var volume = await _repository.GetPoolVolumeAsync(
            poolAddress,
            now.AddDays(-1),
            now);

        // Assert
        volume.Should().Be(150m);
    }

    [Fact]
    public async Task GetRecentSwapsAsync_ShouldReturnOrderedByTimestamp()
    {
        // Arrange
        var poolAddress = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852";
        var now = DateTime.UtcNow;

        await _repository.AddRangeAsync(new[]
        {
            CreateSwapEvent(poolAddress, 100m, "0x1", now.AddHours(-3)),
            CreateSwapEvent(poolAddress, 200m, "0x2", now.AddHours(-1)),
            CreateSwapEvent(poolAddress, 50m, "0x3", now.AddHours(-2))
        });

        // Act
        var swaps = await _repository.GetRecentSwapsAsync(poolAddress, 10);

        // Assert
        swaps.Should().HaveCount(3);
        swaps.First().AmountUsd.Should().Be(200m); // Most recent first
    }

    [Fact]
    public async Task GetRecentSwapsAsync_ShouldRespectLimit()
    {
        // Arrange
        var poolAddress = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852";
        var now = DateTime.UtcNow;

        await _repository.AddRangeAsync(Enumerable.Range(1, 10)
            .Select(i => CreateSwapEvent(poolAddress, i * 10m, $"0x{i}", now.AddMinutes(-i))));

        // Act
        var swaps = await _repository.GetRecentSwapsAsync(poolAddress, 5);

        // Assert
        swaps.Should().HaveCount(5);
    }

    [Fact]
    public async Task ExistsAsync_WhenExists_ShouldReturnTrue()
    {
        // Arrange
        var txHash = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
        var logIndex = 5;
        var swapEvent = SwapEvent.Create(
            "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852",
            "1",
            txHash,
            1000000,
            logIndex,
            "0xsender",
            "0xrecipient",
            1m,
            -1850m,
            100m,
            DateTime.UtcNow);

        await _repository.AddAsync(swapEvent);

        // Act
        var exists = await _repository.ExistsAsync(txHash, logIndex);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenNotExists_ShouldReturnFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync(
            "0x0000000000000000000000000000000000000000000000000000000000000000",
            0);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteOlderThanAsync_ShouldDeleteOldRecords()
    {
        // Arrange
        var poolAddress = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852";
        var now = DateTime.UtcNow;

        await _repository.AddRangeAsync(new[]
        {
            CreateSwapEvent(poolAddress, 100m, "0x1", now.AddDays(-10)),
            CreateSwapEvent(poolAddress, 200m, "0x2", now.AddDays(-5)),
            CreateSwapEvent(poolAddress, 50m, "0x3", now.AddDays(-1))
        });

        // Act
        var deleted = await _repository.DeleteOlderThanAsync(now.AddDays(-7));

        // Assert
        deleted.Should().Be(1);
        var remaining = await _context.SwapEvents.CountAsync();
        remaining.Should().Be(2);
    }

    private static SwapEvent CreateSwapEvent(
        string poolAddress,
        decimal amountUsd,
        string? txHash = null,
        DateTime? timestamp = null)
    {
        return SwapEvent.Create(
            poolAddress,
            "1",
            txHash ?? $"0x{Guid.NewGuid():N}",
            1000000,
            0,
            "0x0000000000000000000000000000000000000001",
            "0x0000000000000000000000000000000000000002",
            1m,
            -1850m,
            amountUsd,
            timestamp ?? DateTime.UtcNow);
    }
}
