using System.Net;
using FluentAssertions;
using Xunit;

namespace AnalyzerCore.Api.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for health check endpoints.
/// </summary>
public class HealthCheckTests : IntegrationTestBase
{
    public HealthCheckTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnOk()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LivenessEndpoint_ShouldReturnOk()
    {
        // Act
        var response = await Client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadinessEndpoint_ShouldReturnOk()
    {
        // Act
        var response = await Client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnJsonContent()
    {
        // Act
        var response = await Client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Contain("status");
    }
}
