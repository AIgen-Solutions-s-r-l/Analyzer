using System.Security.Claims;
using System.Text.Encodings.Web;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalyzerCore.Infrastructure.Authentication;

/// <summary>
/// Authentication handler for API Key authentication.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ApiKeyQueryParamName = "api_key";

    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IApiKeyRepository apiKeyRepository,
        IPasswordHasher passwordHasher)
        : base(options, logger, encoder, clock)
    {
        _apiKeyRepository = apiKeyRepository;
        _passwordHasher = passwordHasher;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Try to get API key from header first, then query string
        string? apiKeyValue = null;

        if (Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerValue))
        {
            apiKeyValue = headerValue.FirstOrDefault();
        }

        if (string.IsNullOrEmpty(apiKeyValue) &&
            Request.Query.TryGetValue(ApiKeyQueryParamName, out var queryValue))
        {
            apiKeyValue = queryValue.FirstOrDefault();
        }

        if (string.IsNullOrEmpty(apiKeyValue))
        {
            return AuthenticateResult.NoResult();
        }

        // Extract prefix (first 8 characters) for lookup
        if (apiKeyValue.Length < 8)
        {
            return AuthenticateResult.Fail("Invalid API key format.");
        }

        var prefix = apiKeyValue[..8];
        var potentialKeys = await _apiKeyRepository.GetByPrefixAsync(prefix);

        // Find the matching key
        ApiKey? matchedKey = null;
        foreach (var key in potentialKeys)
        {
            if (_passwordHasher.Verify(apiKeyValue, key.KeyHash))
            {
                matchedKey = key;
                break;
            }
        }

        if (matchedKey is null)
        {
            Logger.LogWarning("Invalid API key attempt with prefix: {Prefix}", prefix);
            return AuthenticateResult.Fail("Invalid API key.");
        }

        // Check if key is active
        if (!matchedKey.IsActive)
        {
            Logger.LogWarning("Attempted use of revoked API key: {KeyId}", matchedKey.Id);
            return AuthenticateResult.Fail("API key has been revoked.");
        }

        // Check expiration
        if (matchedKey.ExpiresAt.HasValue && matchedKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            Logger.LogWarning("Attempted use of expired API key: {KeyId}", matchedKey.Id);
            return AuthenticateResult.Fail("API key has expired.");
        }

        // Record usage and check rate limit
        var usageResult = matchedKey.RecordUsage();
        if (!usageResult.IsSuccess)
        {
            Logger.LogWarning(
                "API key rate limit exceeded: {KeyId}, Requests: {Requests}/{Limit}",
                matchedKey.Id,
                matchedKey.RequestsToday,
                matchedKey.DailyRateLimit);
            return AuthenticateResult.Fail(usageResult.Error.Message);
        }

        // Save the usage update
        await _apiKeyRepository.UpdateAsync(matchedKey);
        await _apiKeyRepository.SaveChangesAsync();

        Logger.LogInformation(
            "API key authenticated: {KeyName} ({KeyId})",
            matchedKey.Name,
            matchedKey.Id);

        // Create claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, matchedKey.UserId.ToString()),
            new(ClaimTypes.Name, matchedKey.Name),
            new("api_key_id", matchedKey.Id.ToString()),
            new("api_key_scope", matchedKey.Scope.ToString())
        };

        // Add role based on scope
        var role = matchedKey.Scope switch
        {
            ApiKeyScope.Admin => "Admin",
            ApiKeyScope.ReadWrite => "User",
            _ => "ReadOnly"
        };
        claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers["WWW-Authenticate"] = $"{ApiKeyHeaderName} realm=\"API\"";
        return base.HandleChallengeAsync(properties);
    }
}

/// <summary>
/// Options for API Key authentication.
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

/// <summary>
/// API Key authentication scheme constants.
/// </summary>
public static class ApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "ApiKey";
}
