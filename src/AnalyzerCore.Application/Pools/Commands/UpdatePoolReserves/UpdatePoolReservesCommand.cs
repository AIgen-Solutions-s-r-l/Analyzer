using AnalyzerCore.Application.Abstractions.Messaging;

namespace AnalyzerCore.Application.Pools.Commands.UpdatePoolReserves;

/// <summary>
/// Command to update pool reserves.
/// </summary>
public sealed record UpdatePoolReservesCommand : ICommand
{
    public required string Address { get; init; }
    public required string Factory { get; init; }
    public required decimal Reserve0 { get; init; }
    public required decimal Reserve1 { get; init; }
}
