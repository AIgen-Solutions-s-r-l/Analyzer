using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace AnalyzerCore.Api.IntegrationTests;

/// <summary>
/// Base class for integration tests providing common functionality.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected readonly JsonSerializerOptions JsonOptions;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Creates a new service scope for accessing DI services.
    /// </summary>
    protected IServiceScope CreateScope() => Factory.Services.CreateScope();

    /// <summary>
    /// Sets the authorization header with a test API key.
    /// </summary>
    protected void SetApiKey(string apiKey)
    {
        Client.DefaultRequestHeaders.Remove("X-API-Key");
        Client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    /// <summary>
    /// Sets the authorization header with a JWT bearer token.
    /// </summary>
    protected void SetBearerToken(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Clears all authorization headers.
    /// </summary>
    protected void ClearAuth()
    {
        Client.DefaultRequestHeaders.Authorization = null;
        Client.DefaultRequestHeaders.Remove("X-API-Key");
    }

    /// <summary>
    /// Creates JSON content for HTTP requests.
    /// </summary>
    protected StringContent CreateJsonContent<T>(T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Deserializes JSON response content.
    /// </summary>
    protected async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    public void Dispose()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}
