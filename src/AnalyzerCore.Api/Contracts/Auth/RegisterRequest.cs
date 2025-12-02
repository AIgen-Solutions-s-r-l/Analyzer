using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Api.Contracts.Auth;

/// <summary>
/// Request to register a new user.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>
    /// The user's email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    /// <summary>
    /// The user's display name.
    /// </summary>
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string DisplayName { get; init; } = null!;

    /// <summary>
    /// The user's password.
    /// </summary>
    [Required]
    [MinLength(8)]
    public string Password { get; init; } = null!;

    /// <summary>
    /// Confirm password must match password.
    /// </summary>
    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; init; } = null!;
}
