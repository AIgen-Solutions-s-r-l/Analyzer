using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Api.Contracts.Auth;

/// <summary>
/// Request to refresh an access token.
/// </summary>
public sealed class RefreshTokenRequest
{
    /// <summary>
    /// The refresh token received during login.
    /// </summary>
    [Required]
    public string RefreshToken { get; init; } = null!;
}
