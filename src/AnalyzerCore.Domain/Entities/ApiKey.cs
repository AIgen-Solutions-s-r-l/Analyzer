using AnalyzerCore.Domain.Abstractions;

namespace AnalyzerCore.Domain.Entities;

/// <summary>
/// Represents an API Key for machine-to-machine authentication.
/// </summary>
public class ApiKey : AggregateRoot<Guid>
{
    // Private constructor for EF Core
    private ApiKey() { }

    /// <summary>
    /// The hashed key value.
    /// </summary>
    public string KeyHash { get; private set; } = null!;

    /// <summary>
    /// The key prefix (first 8 characters) for identification.
    /// </summary>
    public string KeyPrefix { get; private set; } = null!;

    /// <summary>
    /// A descriptive name for this API Key.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// The user who owns this API Key.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The scopes/permissions granted to this key.
    /// </summary>
    public ApiKeyScope Scope { get; private set; }

    /// <summary>
    /// When this key was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this key expires (null = never).
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// When this key was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; private set; }

    /// <summary>
    /// Whether this key is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// When this key was revoked (if revoked).
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Number of requests made with this key today.
    /// </summary>
    public int RequestsToday { get; private set; }

    /// <summary>
    /// Maximum requests per day (0 = unlimited).
    /// </summary>
    public int DailyRateLimit { get; private set; }

    /// <summary>
    /// The date for which RequestsToday applies.
    /// </summary>
    public DateTime RateLimitResetDate { get; private set; }

    /// <summary>
    /// Creates a new API Key.
    /// </summary>
    public static Result<(ApiKey ApiKey, string PlainTextKey)> Create(
        string name,
        Guid userId,
        string keyHash,
        string plainTextKey,
        ApiKeyScope scope = ApiKeyScope.ReadOnly,
        DateTime? expiresAt = null,
        int dailyRateLimit = 1000)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<(ApiKey, string)>(
                Error.Validation("ApiKey.InvalidName", "Name is required."));

        if (string.IsNullOrWhiteSpace(keyHash))
            return Result.Failure<(ApiKey, string)>(
                Error.Validation("ApiKey.InvalidKeyHash", "Key hash is required."));

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            UserId = userId,
            KeyHash = keyHash,
            KeyPrefix = plainTextKey[..8],
            Scope = scope,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsActive = true,
            DailyRateLimit = dailyRateLimit,
            RateLimitResetDate = DateTime.UtcNow.Date
        };

        return Result.Success((apiKey, plainTextKey));
    }

    /// <summary>
    /// Records a use of this API Key.
    /// </summary>
    public Result RecordUsage()
    {
        if (!IsActive)
            return Result.Failure(Error.Validation("ApiKey.Inactive", "API Key is not active."));

        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow)
            return Result.Failure(Error.Validation("ApiKey.Expired", "API Key has expired."));

        // Reset daily counter if it's a new day
        if (RateLimitResetDate < DateTime.UtcNow.Date)
        {
            RequestsToday = 0;
            RateLimitResetDate = DateTime.UtcNow.Date;
        }

        // Check rate limit
        if (DailyRateLimit > 0 && RequestsToday >= DailyRateLimit)
            return Result.Failure(Error.Validation("ApiKey.RateLimitExceeded", "Daily rate limit exceeded."));

        LastUsedAt = DateTime.UtcNow;
        RequestsToday++;

        return Result.Success();
    }

    /// <summary>
    /// Revokes this API Key.
    /// </summary>
    public Result Revoke()
    {
        if (!IsActive)
            return Result.Failure(Error.Validation("ApiKey.AlreadyRevoked", "API Key is already revoked."));

        IsActive = false;
        RevokedAt = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Checks if this key has the required scope.
    /// </summary>
    public bool HasScope(ApiKeyScope requiredScope)
    {
        return Scope >= requiredScope;
    }
}

/// <summary>
/// API Key permission scopes.
/// </summary>
[Flags]
public enum ApiKeyScope
{
    /// <summary>
    /// Read-only access to public data.
    /// </summary>
    ReadOnly = 1,

    /// <summary>
    /// Read and write access.
    /// </summary>
    ReadWrite = 2,

    /// <summary>
    /// Full administrative access.
    /// </summary>
    Admin = 4
}
