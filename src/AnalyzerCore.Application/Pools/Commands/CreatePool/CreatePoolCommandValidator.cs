using AnalyzerCore.Domain.ValueObjects;
using FluentValidation;

namespace AnalyzerCore.Application.Pools.Commands.CreatePool;

/// <summary>
/// Validator for CreatePoolCommand.
/// </summary>
public sealed class CreatePoolCommandValidator : AbstractValidator<CreatePoolCommand>
{
    public CreatePoolCommandValidator()
    {
        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Pool address is required.")
            .Must(BeValidEthereumAddress)
            .WithMessage("Pool address must be a valid Ethereum address.");

        RuleFor(x => x.Token0Address)
            .NotEmpty()
            .WithMessage("Token0 address is required.")
            .Must(BeValidEthereumAddress)
            .WithMessage("Token0 address must be a valid Ethereum address.");

        RuleFor(x => x.Token1Address)
            .NotEmpty()
            .WithMessage("Token1 address is required.")
            .Must(BeValidEthereumAddress)
            .WithMessage("Token1 address must be a valid Ethereum address.")
            .NotEqual(x => x.Token0Address)
            .WithMessage("Token0 and Token1 addresses must be different.");

        RuleFor(x => x.Factory)
            .NotEmpty()
            .WithMessage("Factory address is required.")
            .Must(BeValidEthereumAddress)
            .WithMessage("Factory address must be a valid Ethereum address.");

        RuleFor(x => x.ChainId)
            .NotEmpty()
            .WithMessage("Chain ID is required.")
            .Must(BeValidChainId)
            .WithMessage("Chain ID must be a valid positive number.");
    }

    private static bool BeValidEthereumAddress(string? address) =>
        EthereumAddress.IsValid(address);

    private static bool BeValidChainId(string? chainId) =>
        !string.IsNullOrWhiteSpace(chainId) &&
        long.TryParse(chainId, out var value) &&
        value > 0;
}
