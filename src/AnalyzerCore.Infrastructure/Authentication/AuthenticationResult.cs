namespace AnalyzerCore.Infrastructure.Authentication;

/// <summary>
/// Result of an authentication operation.
/// </summary>
public sealed record AuthenticationResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid UserId,
    string Email,
    string DisplayName,
    string Role);
