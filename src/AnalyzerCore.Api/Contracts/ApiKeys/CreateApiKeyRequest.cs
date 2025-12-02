using System.ComponentModel.DataAnnotations;
using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Api.Contracts.ApiKeys;

/// <summary>
/// Request to create a new API Key.
/// </summary>
public sealed class CreateApiKeyRequest
{
    /// <summary>
    /// A descriptive name for this API Key.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Name { get; init; } = null!;

    /// <summary>
    /// The permission scope for this key.
    /// </summary>
    public ApiKeyScope Scope { get; init; } = ApiKeyScope.ReadOnly;

    /// <summary>
    /// When this key expires (null = never).
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Maximum requests per day (0 = unlimited).
    /// </summary>
    [Range(0, 1000000)]
    public int DailyRateLimit { get; init; } = 1000;
}
