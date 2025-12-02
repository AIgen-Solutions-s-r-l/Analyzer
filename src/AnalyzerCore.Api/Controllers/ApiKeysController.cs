using System.Security.Claims;
using AnalyzerCore.Api.Contracts.ApiKeys;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerCore.Api.Controllers;

/// <summary>
/// Controller for API Key management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IApiKeyGenerator _apiKeyGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(
        IApiKeyRepository apiKeyRepository,
        IApiKeyGenerator apiKeyGenerator,
        IPasswordHasher passwordHasher,
        ILogger<ApiKeysController> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _apiKeyGenerator = apiKeyGenerator;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Create a new API Key for the current user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateApiKeyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        // Generate the API key
        var (plainTextKey, keyHash) = _apiKeyGenerator.Generate();

        // Create the API key entity
        var createResult = ApiKey.Create(
            request.Name,
            userId.Value,
            keyHash,
            plainTextKey,
            request.Scope,
            request.ExpiresAt,
            request.DailyRateLimit);

        if (!createResult.IsSuccess)
        {
            return BadRequest(new { message = createResult.Error.Message });
        }

        var (apiKey, _) = createResult.Value;

        // Save to database
        await _apiKeyRepository.AddAsync(apiKey, cancellationToken);
        await _apiKeyRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "API Key created: {KeyName} ({KeyId}) for user {UserId}",
            apiKey.Name,
            apiKey.Id,
            userId);

        var response = new CreateApiKeyResponse
        {
            Id = apiKey.Id,
            ApiKey = plainTextKey, // Only shown once!
            KeyPrefix = apiKey.KeyPrefix,
            Name = apiKey.Name,
            Scope = apiKey.Scope.ToString(),
            CreatedAt = apiKey.CreatedAt,
            ExpiresAt = apiKey.ExpiresAt,
            IsActive = apiKey.IsActive,
            DailyRateLimit = apiKey.DailyRateLimit,
            RequestsToday = apiKey.RequestsToday
        };

        return CreatedAtAction(nameof(GetById), new { id = apiKey.Id }, response);
    }

    /// <summary>
    /// Get all API Keys for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ApiKeyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var apiKeys = await _apiKeyRepository.GetByUserIdAsync(userId.Value, cancellationToken);

        var response = apiKeys.Select(k => new ApiKeyResponse
        {
            Id = k.Id,
            KeyPrefix = k.KeyPrefix,
            Name = k.Name,
            Scope = k.Scope.ToString(),
            CreatedAt = k.CreatedAt,
            ExpiresAt = k.ExpiresAt,
            LastUsedAt = k.LastUsedAt,
            IsActive = k.IsActive,
            DailyRateLimit = k.DailyRateLimit,
            RequestsToday = k.RequestsToday
        });

        return Ok(response);
    }

    /// <summary>
    /// Get an API Key by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var apiKey = await _apiKeyRepository.GetByIdAsync(id, cancellationToken);

        if (apiKey is null || apiKey.UserId != userId.Value)
        {
            return NotFound();
        }

        var response = new ApiKeyResponse
        {
            Id = apiKey.Id,
            KeyPrefix = apiKey.KeyPrefix,
            Name = apiKey.Name,
            Scope = apiKey.Scope.ToString(),
            CreatedAt = apiKey.CreatedAt,
            ExpiresAt = apiKey.ExpiresAt,
            LastUsedAt = apiKey.LastUsedAt,
            IsActive = apiKey.IsActive,
            DailyRateLimit = apiKey.DailyRateLimit,
            RequestsToday = apiKey.RequestsToday
        };

        return Ok(response);
    }

    /// <summary>
    /// Revoke an API Key.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var apiKey = await _apiKeyRepository.GetByIdAsync(id, cancellationToken);

        if (apiKey is null || apiKey.UserId != userId.Value)
        {
            return NotFound();
        }

        var revokeResult = apiKey.Revoke();
        if (!revokeResult.IsSuccess)
        {
            return BadRequest(new { message = revokeResult.Error.Message });
        }

        await _apiKeyRepository.UpdateAsync(apiKey, cancellationToken);
        await _apiKeyRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "API Key revoked: {KeyName} ({KeyId}) for user {UserId}",
            apiKey.Name,
            apiKey.Id,
            userId);

        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }
}
