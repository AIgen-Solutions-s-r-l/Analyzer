using AnalyzerCore.Domain.Abstractions;
using FluentAssertions;
using Xunit;

namespace AnalyzerCore.Domain.Tests.Abstractions;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Success_Generic_ShouldCreateSuccessfulResultWithValue()
    {
        // Arrange
        var value = 42;

        // Act
        var result = Result.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void Failure_Generic_ShouldCreateFailedResult()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");

        // Act
        var result = Result.Failure<int>(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Value_OnFailedResult_ShouldThrow()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");
        var result = Result.Failure<int>(error);

        // Act & Assert
        var action = () => _ = result.Value;
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed result*");
    }

    [Fact]
    public void ValueOrDefault_OnSuccessfulResult_ShouldReturnValue()
    {
        // Arrange
        var result = Result.Success(42);

        // Act
        var value = result.ValueOrDefault;

        // Assert
        value.Should().Be(42);
    }

    [Fact]
    public void ValueOrDefault_OnFailedResult_ShouldReturnDefault()
    {
        // Arrange
        var result = Result.Failure<int>(new Error("Test.Error", "Test"));

        // Act
        var value = result.ValueOrDefault;

        // Assert
        value.Should().Be(default(int));
    }

    [Fact]
    public void Map_OnSuccessfulResult_ShouldTransformValue()
    {
        // Arrange
        var result = Result.Success(42);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("42");
    }

    [Fact]
    public void Map_OnFailedResult_ShouldPropagateError()
    {
        // Arrange
        var error = new Error("Test.Error", "Test");
        var result = Result.Failure<int>(error);

        // Act
        var mapped = result.Map(x => x.ToString());

        // Assert
        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be(error);
    }

    [Fact]
    public void Bind_OnSuccessfulResult_ShouldChainResults()
    {
        // Arrange
        var result = Result.Success(42);

        // Act
        var bound = result.Bind(x => Result.Success(x * 2));

        // Assert
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be(84);
    }

    [Fact]
    public void Bind_OnFailedResult_ShouldPropagateError()
    {
        // Arrange
        var error = new Error("Test.Error", "Test");
        var result = Result.Failure<int>(error);

        // Act
        var bound = result.Bind(x => Result.Success(x * 2));

        // Assert
        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(error);
    }

    [Fact]
    public void Match_OnSuccessfulResult_ShouldCallOnSuccess()
    {
        // Arrange
        var result = Result.Success(42);

        // Act
        var matched = result.Match(
            onSuccess: x => $"Success: {x}",
            onFailure: e => $"Failure: {e.Message}");

        // Assert
        matched.Should().Be("Success: 42");
    }

    [Fact]
    public void Match_OnFailedResult_ShouldCallOnFailure()
    {
        // Arrange
        var error = new Error("Test.Error", "Test message");
        var result = Result.Failure<int>(error);

        // Act
        var matched = result.Match(
            onSuccess: x => $"Success: {x}",
            onFailure: e => $"Failure: {e.Message}");

        // Assert
        matched.Should().Be("Failure: Test message");
    }

    [Fact]
    public void Tap_OnSuccessfulResult_ShouldExecuteAction()
    {
        // Arrange
        var result = Result.Success(42);
        var executed = false;

        // Act
        result.Tap(_ => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void Tap_OnFailedResult_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result.Failure<int>(new Error("Test.Error", "Test"));
        var executed = false;

        // Act
        result.Tap(_ => executed = true);

        // Assert
        executed.Should().BeFalse();
    }

    [Fact]
    public void Combine_AllSuccess_ShouldReturnSuccess()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Success();
        var result3 = Result.Success();

        // Act
        var combined = Result.Combine(result1, result2, result3);

        // Assert
        combined.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Combine_OneFailure_ShouldReturnFirstFailure()
    {
        // Arrange
        var result1 = Result.Success();
        var error = new Error("Test.Error", "Test");
        var result2 = Result.Failure(error);
        var result3 = Result.Success();

        // Act
        var combined = Result.Combine(result1, result2, result3);

        // Assert
        combined.IsFailure.Should().BeTrue();
        combined.Error.Should().Be(error);
    }

    [Fact]
    public void Create_WithNonNullValue_ShouldReturnSuccess()
    {
        // Act
        var result = Result.Create("test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test");
    }

    [Fact]
    public void Create_WithNullValue_ShouldReturnFailure()
    {
        // Act
        var result = Result.Create<string>(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NullValue);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccess()
    {
        // Act
        Result<int> result = 42;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldCreateFailure()
    {
        // Arrange
        var error = new Error("Test.Error", "Test");

        // Act
        Result<int> result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
