using AnalyzerCore.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AnalyzerCore.Domain.Tests.ValueObjects;

public class ChainIdTests
{
    [Theory]
    [InlineData("1")]
    [InlineData("137")]
    [InlineData("56")]
    [InlineData("42161")]
    public void Create_WithValidChainId_ShouldSucceed(string chainId)
    {
        // Act
        var result = ChainId.Create(chainId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(chainId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmpty_ShouldFail(string? chainId)
    {
        // Act
        var result = ChainId.Create(chainId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ChainId.NullOrEmpty");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("1.5")]
    public void Create_WithInvalidFormat_ShouldFail(string chainId)
    {
        // Act
        var result = ChainId.Create(chainId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ChainId.InvalidFormat");
    }

    [Fact]
    public void Create_WithWellKnownChainId_ShouldReturnWellKnownInstance()
    {
        // Act
        var result = ChainId.Create("1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(ChainId.Ethereum);
        result.Value.Name.Should().Be("Ethereum Mainnet");
        result.Value.IsWellKnown.Should().BeTrue();
    }

    [Fact]
    public void Create_WithCustomChainId_ShouldReturnCustomInstance()
    {
        // Act
        var result = ChainId.Create("999999");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("999999");
        result.Value.Name.Should().BeNull();
        result.Value.IsWellKnown.Should().BeFalse();
    }

    [Theory]
    [InlineData("1", true, false)]  // Ethereum mainnet
    [InlineData("5", false, true)]  // Goerli testnet
    [InlineData("11155111", false, true)]  // Sepolia testnet
    [InlineData("137", true, false)]  // Polygon mainnet
    public void IsMainnet_And_IsTestnet_ShouldBeCorrect(string chainIdValue, bool isMainnet, bool isTestnet)
    {
        // Arrange
        var chainId = ChainId.Create(chainIdValue).Value;

        // Assert
        chainId.IsMainnet.Should().Be(isMainnet);
        chainId.IsTestnet.Should().Be(isTestnet);
    }

    [Fact]
    public void Equality_SameChainId_ShouldBeEqual()
    {
        // Arrange
        var chainId1 = ChainId.Create("1").Value;
        var chainId2 = ChainId.Create("1").Value;

        // Assert
        chainId1.Should().Be(chainId2);
        (chainId1 == chainId2).Should().BeTrue();
        chainId1.GetHashCode().Should().Be(chainId2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentChainIds_ShouldNotBeEqual()
    {
        // Arrange
        var chainId1 = ChainId.Create("1").Value;
        var chainId2 = ChainId.Create("137").Value;

        // Assert
        chainId1.Should().NotBe(chainId2);
        (chainId1 != chainId2).Should().BeTrue();
    }

    [Fact]
    public void WellKnownChains_ShouldHaveCorrectValues()
    {
        // Assert
        ChainId.Ethereum.Value.Should().Be("1");
        ChainId.Polygon.Value.Should().Be("137");
        ChainId.BinanceSmartChain.Value.Should().Be("56");
        ChainId.Arbitrum.Value.Should().Be("42161");
        ChainId.Optimism.Value.Should().Be("10");
        ChainId.Base.Value.Should().Be("8453");
    }

    [Fact]
    public void GetWellKnown_WithKnownChainId_ShouldReturnInstance()
    {
        // Act
        var result = ChainId.GetWellKnown("1");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(ChainId.Ethereum);
    }

    [Fact]
    public void GetWellKnown_WithUnknownChainId_ShouldReturnNull()
    {
        // Act
        var result = ChainId.GetWellKnown("999999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var chainId = ChainId.Create("1").Value;

        // Act
        string stringValue = chainId;

        // Assert
        stringValue.Should().Be("1");
    }

    [Fact]
    public void ToString_WithWellKnownChain_ShouldReturnName()
    {
        // Arrange
        var chainId = ChainId.Ethereum;

        // Act
        var result = chainId.ToString();

        // Assert
        result.Should().Be("Ethereum Mainnet");
    }

    [Fact]
    public void ToString_WithCustomChain_ShouldReturnValue()
    {
        // Arrange
        var chainId = ChainId.Create("999999").Value;

        // Act
        var result = chainId.ToString();

        // Assert
        result.Should().Be("999999");
    }
}
