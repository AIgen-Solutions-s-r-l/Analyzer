using AnalyzerCore.Application.Behaviors;
using AnalyzerCore.Application.Tokens.Commands.CreateToken;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Entities;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Xunit;

namespace AnalyzerCore.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<CreateTokenCommand>>();
        var behavior = new ValidationBehavior<CreateTokenCommand, Result<Token>>(validators);

        var command = new CreateTokenCommand
        {
            Address = "0x6B175474E89094C44Da98b954EedeAC495271d0F",
            ChainId = "1",
            Symbol = "DAI",
            Name = "Dai",
            Decimals = 18,
            TotalSupply = 0
        };

        var expectedResult = Result.Success(Token.CreateLegacy(
            command.Address, command.Symbol, command.Name, command.Decimals, command.ChainId));

        RequestHandlerDelegate<Result<Token>> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithPassingValidation_ShouldCallNext()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<CreateTokenCommand>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<CreateTokenCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new[] { mockValidator.Object };
        var behavior = new ValidationBehavior<CreateTokenCommand, Result<Token>>(validators);

        var command = new CreateTokenCommand
        {
            Address = "0x6B175474E89094C44Da98b954EedeAC495271d0F",
            ChainId = "1",
            Symbol = "DAI",
            Name = "Dai",
            Decimals = 18,
            TotalSupply = 0
        };

        var nextCalled = false;
        RequestHandlerDelegate<Result<Token>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success(Token.CreateLegacy(
                command.Address, command.Symbol, command.Name, command.Decimals, command.ChainId)));
        };

        // Act
        await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithFailingValidation_ShouldReturnFailure()
    {
        // Arrange
        var validationFailures = new List<ValidationFailure>
        {
            new("Address", "Address is required"),
            new("ChainId", "ChainId is required")
        };

        var mockValidator = new Mock<IValidator<CreateTokenCommand>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<CreateTokenCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        var validators = new[] { mockValidator.Object };
        var behavior = new ValidationBehavior<CreateTokenCommand, Result<Token>>(validators);

        var command = new CreateTokenCommand
        {
            Address = "",
            ChainId = "",
            Symbol = "DAI",
            Name = "Dai",
            Decimals = 18,
            TotalSupply = 0
        };

        var nextCalled = false;
        RequestHandlerDelegate<Result<Token>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success(Token.CreateLegacy(
                "addr", "SYM", "Name", 18, "1")));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Validation.Error");
        nextCalled.Should().BeFalse();
    }
}
