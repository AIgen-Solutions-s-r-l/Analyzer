using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Api.Contracts.Auth;

/// <summary>
/// Request to authenticate a user.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// The user's email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    /// <summary>
    /// The user's password.
    /// </summary>
    [Required]
    [MinLength(8)]
    public string Password { get; init; } = null!;
}
