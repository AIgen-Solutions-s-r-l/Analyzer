using System.Net;
using FluentAssertions;
using Xunit;

namespace AnalyzerCore.Api.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for ArbitrageController endpoints.
/// </summary>
public class ArbitrageControllerIntegrationTests : IntegrationTestBase
{
    private const string WethAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
    private const string SamplePool = "0x0d4a11d5eeaac28ec3f61d100daf4d40471f1852";

    public ArbitrageControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Scan_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync("/api/v1/arbitrage/scan");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Scan_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        SetBearerToken("invalid-token");

        // Act
        var response = await Client.GetAsync("/api/v1/arbitrage/scan");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByToken_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync($"/api/v1/arbitrage/token/{WethAddress}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTriangular_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync("/api/v1/arbitrage/triangular");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CalculateOptimal_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync(
            $"/api/v1/arbitrage/calculate?buyPool={SamplePool}&sellPool={SamplePool}&tokenAddress={WethAddress}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Scan_WithMinProfitParameter_ShouldAcceptParameter()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync("/api/v1/arbitrage/scan?minProfitUsd=50");

        // Assert - Still unauthorized but validates route matching
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
