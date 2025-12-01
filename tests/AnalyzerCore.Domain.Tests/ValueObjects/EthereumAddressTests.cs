using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AnalyzerCore.Domain.Tests.ValueObjects;

public class EthereumAddressTests
{
    [Theory]
    [InlineData("0x742d35Cc6634C0532925a3b844Bc9e7595f1dE3d")]
    [InlineData("0x742d35cc6634c0532925a3b844bc9e7595f1de3d")]
    [InlineData("0x0000000000000000000000000000000000000000")]
    public void Create_WithValidAddress_ShouldSucceed(string address)
    {
        // Act
        var result = EthereumAddress.Create(address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(address.ToLowerInvariant());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldFail(string? address)
    {
        // Act
        var result = EthereumAddress.Create(address);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Address.NullOrEmpty");
    }

    [Theory]
    [InlineData("0x123")] // Too short
    [InlineData("0x742d35Cc6634C0532925a3b844Bc9e7595f1dE3d00")] // Too long
    [InlineData("742d35Cc6634C0532925a3b844Bc9e7595f1dE3d")] // Missing 0x
    public void Create_WithInvalidLength_ShouldFail(string address)
    {
        // Act
        var result = EthereumAddress.Create(address);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Address.");
    }

    [Theory]
    [InlineData("0x742d35Cc6634C0532925a3b844Bc9e7595f1dGGG")] // Invalid hex
    [InlineData("0xZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
    public void Create_WithInvalidHex_ShouldFail(string address)
    {
        // Act
        var result = EthereumAddress.Create(address);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Address.InvalidFormat");
    }

    [Fact]
    public void IsValid_WithValidAddress_ShouldReturnTrue()
    {
        // Arrange
        var address = "0x742d35Cc6634C0532925a3b844Bc9e7595f1dE3d";

        // Act
        var isValid = EthereumAddress.IsValid(address);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithInvalidAddress_ShouldReturnFalse()
    {
        // Arrange
        var address = "invalid";

        // Act
        var isValid = EthereumAddress.IsValid(address);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Equality_SameAddress_ShouldBeEqual()
    {
        // Arrange
        var address1 = EthereumAddress.Create("0x742d35Cc6634C0532925a3b844Bc9e7595f1dE3d").Value;
        var address2 = EthereumAddress.Create("0x742d35cc6634c0532925a3b844bc9e7595f1de3d").Value;

        // Assert
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentAddresses_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = EthereumAddress.Create("0x742d35Cc6634C0532925a3b844Bc9e7595f1dE3d").Value;
        var address2 = EthereumAddress.Create("0x0000000000000000000000000000000000000000").Value;

        // Assert
        address1.Should().NotBe(address2);
        (address1 != address2).Should().BeTrue();
    }

    [Fact]
    public void Zero_ShouldBeZeroAddress()
    {
        // Assert
        EthereumAddress.Zero.Value.Should().Be("0x0000000000000000000000000000000000000000");
        EthereumAddress.Zero.IsZeroAddress().Should().BeTrue();
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var address = EthereumAddress.Create("0x742d35Cc6634C0532925a3b844Bc9e7595f1dE3d").Value;

        // Act
        string stringValue = address;

        // Assert
        stringValue.Should().Be(address.Value);
    }

    [Fact]
    public void FromTrusted_WithValidAddress_ShouldNotThrow()
    {
        // Arrange
        var address = "0x742d35Cc6634C0532925a3b844Bc9e7595f1dE3d";

        // Act
        var result = EthereumAddress.FromTrusted(address);

        // Assert
        result.Value.Should().Be(address.ToLowerInvariant());
    }

    [Fact]
    public void FromTrusted_WithInvalidAddress_ShouldThrow()
    {
        // Arrange
        var address = "invalid";

        // Act & Assert
        var action = () => EthereumAddress.FromTrusted(address);
        action.Should().Throw<ArgumentException>();
    }
}
