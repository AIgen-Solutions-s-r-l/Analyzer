using AnalyzerCore.Infrastructure.Persistence;
using AnalyzerCore.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AnalyzerCore.Infrastructure.Tests.Services;

public class IdempotencyServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IdempotencyService _service;

    public IdempotencyServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"IdempotencyTestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _service = new IdempotencyService(_dbContext);
    }

    [Fact]
    public async Task RequestExistsAsync_WhenRequestDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        // Act
        var result = await _service.RequestExistsAsync(requestId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RequestExistsAsync_WhenRequestExists_ShouldReturnTrue()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        await _service.CreateRequestAsync(requestId, "TestRequest");

        // Act
        var result = await _service.RequestExistsAsync(requestId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CreateRequestAsync_ShouldCreateRequest()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var requestName = "TestRequest";

        // Act
        await _service.CreateRequestAsync(requestId, requestName);

        // Assert
        var request = await _dbContext.IdempotentRequests.FindAsync(requestId);
        request.Should().NotBeNull();
        request!.Name.Should().Be(requestName);
    }

    [Fact]
    public async Task CreateRequestAsync_ShouldSetCreatedAtToUtcNow()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var beforeCreate = DateTime.UtcNow;

        // Act
        await _service.CreateRequestAsync(requestId, "TestRequest");

        // Assert
        var afterCreate = DateTime.UtcNow;
        var request = await _dbContext.IdempotentRequests.FindAsync(requestId);
        request!.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        request.CreatedAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public async Task CreateRequestAsync_WhenDuplicateId_ShouldThrowDbUpdateException()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        await _service.CreateRequestAsync(requestId, "FirstRequest");

        // Act
        var act = () => _service.CreateRequestAsync(requestId, "SecondRequest");

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task RequestExistsAsync_WithMultipleRequests_ShouldOnlyMatchSpecificId()
    {
        // Arrange
        var requestId1 = Guid.NewGuid();
        var requestId2 = Guid.NewGuid();
        var requestId3 = Guid.NewGuid();
        await _service.CreateRequestAsync(requestId1, "Request1");
        await _service.CreateRequestAsync(requestId2, "Request2");

        // Act
        var exists1 = await _service.RequestExistsAsync(requestId1);
        var exists2 = await _service.RequestExistsAsync(requestId2);
        var exists3 = await _service.RequestExistsAsync(requestId3);

        // Assert
        exists1.Should().BeTrue();
        exists2.Should().BeTrue();
        exists3.Should().BeFalse();
    }

    [Fact]
    public async Task CreateRequestAsync_ShouldSupportCancellation()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _service.CreateRequestAsync(requestId, "TestRequest", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RequestExistsAsync_ShouldSupportCancellation()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = () => _service.RequestExistsAsync(requestId, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
