using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Infrastructure.Authentication;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates an access token for the specified user.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Gets the expiration time for refresh tokens.
    /// </summary>
    DateTime GetRefreshTokenExpiration();
}
