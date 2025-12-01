using AnalyzerCore.Domain.ValueObjects;
using FluentValidation;

namespace AnalyzerCore.Application.Pools.Commands.UpdatePoolReserves;

/// <summary>
/// Validator for UpdatePoolReservesCommand.
/// </summary>
public sealed class UpdatePoolReservesCommandValidator : AbstractValidator<UpdatePoolReservesCommand>
{
    public UpdatePoolReservesCommandValidator()
    {
        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Pool address is required.")
            .Must(BeValidEthereumAddress)
            .WithMessage("Pool address must be a valid Ethereum address.");

        RuleFor(x => x.Factory)
            .NotEmpty()
            .WithMessage("Factory address is required.")
            .Must(BeValidEthereumAddress)
            .WithMessage("Factory address must be a valid Ethereum address.");

        RuleFor(x => x.Reserve0)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Reserve0 cannot be negative.");

        RuleFor(x => x.Reserve1)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Reserve1 cannot be negative.");
    }

    private static bool BeValidEthereumAddress(string? address) =>
        EthereumAddress.IsValid(address);
}
