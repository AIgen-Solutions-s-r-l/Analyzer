using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Events;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Domain.Entities;

/// <summary>
/// Represents an ERC20 token on a blockchain.
/// </summary>
public class Token : AggregateRoot<int>
{
    // Private constructor for EF Core
    private Token() { }

    /// <summary>
    /// The token's contract address.
    /// </summary>
    public string Address { get; private set; } = null!;

    /// <summary>
    /// The token's symbol (e.g., "ETH", "USDC").
    /// </summary>
    public string Symbol { get; private set; } = null!;

    /// <summary>
    /// The token's full name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// The number of decimals the token uses.
    /// </summary>
    public int Decimals { get; private set; }

    /// <summary>
    /// The total supply of the token.
    /// </summary>
    public decimal TotalSupply { get; private set; }

    /// <summary>
    /// The chain ID where this token exists.
    /// </summary>
    public string ChainId { get; private set; } = null!;

    /// <summary>
    /// When this token was first discovered.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Whether this token is a placeholder with incomplete information.
    /// </summary>
    public bool IsPlaceholder { get; private set; }

    /// <summary>
    /// When the token information was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a new Token entity with validated parameters.
    /// </summary>
    public static Result<Token> Create(
        EthereumAddress address,
        string symbol,
        string name,
        int decimals,
        decimal totalSupply,
        ChainId chainId)
    {
        // Validate symbol
        if (string.IsNullOrWhiteSpace(symbol))
            return Result.Failure<Token>(DomainErrors.Token.InvalidSymbol);

        // Validate name
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Token>(DomainErrors.Token.InvalidName);

        // Validate decimals
        if (decimals < 0 || decimals > 18)
            return Result.Failure<Token>(DomainErrors.Token.InvalidDecimals);

        var token = new Token
        {
            Address = address.Value,
            Symbol = symbol.ToUpperInvariant(),
            Name = name.Trim(),
            Decimals = decimals,
            TotalSupply = totalSupply,
            ChainId = chainId.Value,
            CreatedAt = DateTime.UtcNow,
            IsPlaceholder = false
        };

        token.RaiseDomainEvent(new TokenCreatedDomainEvent(
            token.Address,
            token.Symbol,
            token.Name,
            token.Decimals,
            token.ChainId));

        return Result.Success(token);
    }

    /// <summary>
    /// Creates a placeholder token for an unknown address.
    /// The token information should be updated later when available.
    /// </summary>
    public static Result<Token> CreatePlaceholder(EthereumAddress address, ChainId chainId)
    {
        var token = new Token
        {
            Address = address.Value,
            Symbol = "UNKNOWN",
            Name = "Unknown Token",
            Decimals = 18, // Default assumption
            TotalSupply = 0,
            ChainId = chainId.Value,
            CreatedAt = DateTime.UtcNow,
            IsPlaceholder = true
        };

        return Result.Success(token);
    }

    /// <summary>
    /// Legacy factory method for backward compatibility.
    /// </summary>
    [Obsolete("Use Create(EthereumAddress, ...) instead")]
    public static Token Create(
        string address,
        string symbol,
        string name,
        int decimals,
        decimal totalSupply,
        string chainId)
    {
        return new Token
        {
            Address = address.ToLowerInvariant(),
            Symbol = symbol.ToUpperInvariant(),
            Name = name,
            Decimals = decimals,
            TotalSupply = totalSupply,
            ChainId = chainId,
            CreatedAt = DateTime.UtcNow,
            IsPlaceholder = false
        };
    }

    /// <summary>
    /// Updates the token information from blockchain data.
    /// Only allowed for placeholder tokens.
    /// </summary>
    public Result UpdateInfo(string symbol, string name, int decimals, decimal totalSupply)
    {
        if (!IsPlaceholder)
            return Result.Failure(Error.Validation("Cannot update a non-placeholder token."));

        if (string.IsNullOrWhiteSpace(symbol))
            return Result.Failure(DomainErrors.Token.InvalidSymbol);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(DomainErrors.Token.InvalidName);

        if (decimals < 0 || decimals > 18)
            return Result.Failure(DomainErrors.Token.InvalidDecimals);

        var oldSymbol = Symbol;
        var oldName = Name;

        Symbol = symbol.ToUpperInvariant();
        Name = name.Trim();
        Decimals = decimals;
        TotalSupply = totalSupply;
        IsPlaceholder = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TokenInfoUpdatedDomainEvent(
            Address,
            oldSymbol,
            Symbol,
            oldName,
            Name));

        return Result.Success();
    }

    /// <summary>
    /// Gets the EthereumAddress value object for this token.
    /// </summary>
    public EthereumAddress GetAddress() => EthereumAddress.FromTrusted(Address);

    /// <summary>
    /// Gets the ChainId value object for this token.
    /// </summary>
    public ChainId GetChainId() => ChainId.FromTrusted(ChainId);
}
