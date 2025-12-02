using System.Net;
using FluentAssertions;
using Xunit;

namespace AnalyzerCore.Api.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for ApiKeysController endpoints.
/// </summary>
public class ApiKeysControllerIntegrationTests : IntegrationTestBase
{
    public ApiKeysControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateApiKey_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();
        var request = new
        {
            name = "Test API Key",
            permissions = new[] { "read" }
        };

        // Act
        var response = await Client.PostAsync(
            "/api/v1/api-keys",
            CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListApiKeys_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.GetAsync("/api/v1/api-keys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeApiKey_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuth();
        var keyId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/v1/api-keys/{keyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateApiKey_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        SetBearerToken("invalid-token");
        var request = new
        {
            name = "Test API Key",
            permissions = new[] { "read" }
        };

        // Act
        var response = await Client.PostAsync(
            "/api/v1/api-keys",
            CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateApiKey_WithEmptyBody_ShouldRequireAuthFirst()
    {
        // Arrange
        ClearAuth();

        // Act
        var response = await Client.PostAsync(
            "/api/v1/api-keys",
            CreateJsonContent(new { }));

        // Assert - Auth check happens before validation
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
