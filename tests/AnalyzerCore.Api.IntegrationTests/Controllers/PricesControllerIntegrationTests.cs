using System.Net;
using FluentAssertions;
using Xunit;

namespace AnalyzerCore.Api.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for PricesController endpoints.
/// </summary>
public class PricesControllerIntegrationTests : IntegrationTestBase
{
    private const string WethAddress = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";

    public PricesControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetQuoteCurrencies_ShouldReturnOk_WithoutAuth()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/prices/quote-currencies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPrice_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync($"/api/v1/prices/{WethAddress}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPrice_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        SetBearerToken("invalid-token");

        // Act
        var response = await Client.GetAsync($"/api/v1/prices/{WethAddress}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsdPrice_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync($"/api/v1/prices/{WethAddress}/usd");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTwap_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync($"/api/v1/prices/{WethAddress}/twap");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPriceHistory_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync($"/api/v1/prices/{WethAddress}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
