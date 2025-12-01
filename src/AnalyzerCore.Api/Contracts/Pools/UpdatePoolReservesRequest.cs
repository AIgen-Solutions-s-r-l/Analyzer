using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Api.Contracts.Pools;

/// <summary>
/// Request to update pool reserves.
/// </summary>
public sealed record UpdatePoolReservesRequest
{
    /// <summary>
    /// The reserve amount of token0.
    /// </summary>
    /// <example>1000000.123456789012345678</example>
    [Required]
    [Range(0, double.MaxValue)]
    public decimal Reserve0 { get; init; }

    /// <summary>
    /// The reserve amount of token1.
    /// </summary>
    /// <example>2500000.987654321098765432</example>
    [Required]
    [Range(0, double.MaxValue)]
    public decimal Reserve1 { get; init; }
}
