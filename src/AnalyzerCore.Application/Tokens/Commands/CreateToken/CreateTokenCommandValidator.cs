using AnalyzerCore.Domain.ValueObjects;
using FluentValidation;

namespace AnalyzerCore.Application.Tokens.Commands.CreateToken;

/// <summary>
/// Validator for CreateTokenCommand.
/// </summary>
public sealed class CreateTokenCommandValidator : AbstractValidator<CreateTokenCommand>
{
    public CreateTokenCommandValidator()
    {
        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Token address is required.")
            .Must(BeValidEthereumAddress)
            .WithMessage("Token address must be a valid Ethereum address.");

        RuleFor(x => x.Symbol)
            .NotEmpty()
            .WithMessage("Token symbol is required.")
            .MaximumLength(20)
            .WithMessage("Token symbol cannot exceed 20 characters.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Token name is required.")
            .MaximumLength(100)
            .WithMessage("Token name cannot exceed 100 characters.");

        RuleFor(x => x.Decimals)
            .InclusiveBetween(0, 18)
            .WithMessage("Token decimals must be between 0 and 18.");

        RuleFor(x => x.TotalSupply)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Total supply cannot be negative.");

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
