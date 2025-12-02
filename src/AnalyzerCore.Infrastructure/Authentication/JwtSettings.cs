namespace AnalyzerCore.Infrastructure.Authentication;

/// <summary>
/// JWT configuration settings.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// The secret key used for signing tokens.
    /// Must be at least 32 characters for HS256.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The token issuer (typically the API URL).
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// The token audience (typically the client application).
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time in minutes.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Refresh token expiration time in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
