namespace AnalyzerCore.Api.Contracts.Auth;

/// <summary>
/// Authentication response containing tokens.
/// </summary>
public sealed class AuthResponse
{
    /// <summary>
    /// The JWT access token.
    /// </summary>
    public string AccessToken { get; init; } = null!;

    /// <summary>
    /// The refresh token for obtaining new access tokens.
    /// </summary>
    public string RefreshToken { get; init; } = null!;

    /// <summary>
    /// When the access token expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// The user's ID.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// The user's email.
    /// </summary>
    public string Email { get; init; } = null!;

    /// <summary>
    /// The user's display name.
    /// </summary>
    public string DisplayName { get; init; } = null!;

    /// <summary>
    /// The user's role.
    /// </summary>
    public string Role { get; init; } = null!;
}
