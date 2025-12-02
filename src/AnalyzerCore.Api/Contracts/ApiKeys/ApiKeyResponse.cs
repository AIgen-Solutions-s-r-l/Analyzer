namespace AnalyzerCore.Api.Contracts.ApiKeys;

/// <summary>
/// API Key response (without the actual key value).
/// </summary>
public sealed class ApiKeyResponse
{
    /// <summary>
    /// The unique identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The key prefix (first 8 characters) for identification.
    /// </summary>
    public string KeyPrefix { get; init; } = null!;

    /// <summary>
    /// A descriptive name for this API Key.
    /// </summary>
    public string Name { get; init; } = null!;

    /// <summary>
    /// The permission scope.
    /// </summary>
    public string Scope { get; init; } = null!;

    /// <summary>
    /// When this key was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When this key expires (null = never).
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// When this key was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; init; }

    /// <summary>
    /// Whether this key is active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Number of requests made with this key today.
    /// </summary>
    public int RequestsToday { get; init; }

    /// <summary>
    /// Maximum requests per day.
    /// </summary>
    public int DailyRateLimit { get; init; }
}

/// <summary>
/// Response when creating a new API Key (includes the key value once).
/// </summary>
public sealed class CreateApiKeyResponse : ApiKeyResponse
{
    /// <summary>
    /// The API Key value. This is only shown once at creation time.
    /// </summary>
    public string ApiKey { get; init; } = null!;
}
