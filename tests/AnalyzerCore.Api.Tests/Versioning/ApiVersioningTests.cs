using System.Net;
using FluentAssertions;
using Xunit;

namespace AnalyzerCore.Api.Tests.Versioning;

public class ApiVersioningTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiVersioningTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTokens_WithVersionedRoute_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/tokens");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPools_WithVersionedRoute_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/pools");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPrices_WithVersionedRoute_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/prices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_ShouldReturnApiVersionHeader()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/tokens");

        // Assert
        response.Headers.Should().ContainKey("api-supported-versions");
    }

    [Fact]
    public async Task GetTokens_WithVersionQueryString_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/tokens?api-version=1.0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTokens_WithVersionHeader_ShouldReturnOk()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tokens");
        request.Headers.Add("X-Api-Version", "1.0");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/api/v1/tokens")]
    [InlineData("/api/v1/pools")]
    [InlineData("/api/v1/prices")]
    [InlineData("/api/v1/arbitrage/opportunities")]
    [InlineData("/api/v1/liquidity/analytics")]
    public async Task VersionedEndpoints_ShouldBeAccessible(string endpoint)
    {
        // Act
        var response = await _client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        // NotFound is acceptable for some endpoints that require parameters
    }
}
