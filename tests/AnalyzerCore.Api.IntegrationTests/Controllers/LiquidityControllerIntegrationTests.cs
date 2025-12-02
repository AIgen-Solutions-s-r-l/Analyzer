using System.Net;
using FluentAssertions;
using Xunit;

namespace AnalyzerCore.Api.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for LiquidityController endpoints.
/// </summary>
public class LiquidityControllerIntegrationTests : IntegrationTestBase
{
    private const string WethAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
    private const string SamplePool = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852";

    public LiquidityControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetPoolMetrics_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync($"/api/v1/liquidity/pools/{SamplePool}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTokenLiquidity_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync($"/api/v1/liquidity/tokens/{WethAddress}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTopPools_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync("/api/v1/liquidity/top-pools");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTopPools_WithLimitParameter_ShouldAcceptParameter()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync("/api/v1/liquidity/top-pools?limit=5");

        // Assert - Still unauthorized but validates route matching
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CalculateImpermanentLoss_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();
        var request = new
        {
            poolAddress = SamplePool,
            entryPriceRatio = 1850.00m,
            initialInvestmentUsd = 10000.00m
        };

        // Act
        var response = await Client.PostAsync(
            "/api/v1/liquidity/impermanent-loss",
            CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLiquidityConcentration_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync($"/api/v1/liquidity/concentration/{WethAddress}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPoolMetrics_WithInvalidPoolAddress_ShouldStillRequireAuth()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync("/api/v1/liquidity/pools/invalid-address");

        // Assert - Auth check happens before validation
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
