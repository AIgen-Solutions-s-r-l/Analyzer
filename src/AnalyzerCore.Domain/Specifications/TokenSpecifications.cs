using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Domain.Specifications;

/// <summary>
/// Specification to get a token by address and chain ID.
/// </summary>
public sealed class TokenByAddressSpecification : BaseSpecification<Token>
{
    public TokenByAddressSpecification(string address, string chainId)
        : base(t => t.Address == address.ToLowerInvariant() && t.ChainId == chainId)
    {
    }
}

/// <summary>
/// Specification to get all tokens by chain ID.
/// </summary>
public sealed class TokensByChainIdSpecification : BaseSpecification<Token>
{
    public TokensByChainIdSpecification(string chainId)
        : base(t => t.ChainId == chainId)
    {
        ApplyOrderByDescending(t => t.CreatedAt);
    }
}

/// <summary>
/// Specification to get tokens by symbol.
/// </summary>
public sealed class TokensBySymbolSpecification : BaseSpecification<Token>
{
    public TokensBySymbolSpecification(string symbol, string chainId)
        : base(t => t.Symbol == symbol.ToUpperInvariant() && t.ChainId == chainId)
    {
    }
}

/// <summary>
/// Specification to get placeholder tokens that need updating.
/// </summary>
public sealed class PlaceholderTokensSpecification : BaseSpecification<Token>
{
    public PlaceholderTokensSpecification(string chainId)
        : base(t => t.IsPlaceholder && t.ChainId == chainId)
    {
        ApplyOrderBy(t => t.CreatedAt);
    }
}

/// <summary>
/// Specification to get tokens created after a specific timestamp.
/// </summary>
public sealed class TokensCreatedAfterSpecification : BaseSpecification<Token>
{
    public TokensCreatedAfterSpecification(DateTime timestamp, string chainId)
        : base(t => t.CreatedAt >= timestamp && t.ChainId == chainId)
    {
        ApplyOrderByDescending(t => t.CreatedAt);
    }
}

/// <summary>
/// Specification to search tokens by name or symbol.
/// </summary>
public sealed class TokenSearchSpecification : BaseSpecification<Token>
{
    public TokenSearchSpecification(string searchTerm, string chainId)
        : base(t =>
            t.ChainId == chainId &&
            (t.Name.Contains(searchTerm) || t.Symbol.Contains(searchTerm.ToUpperInvariant())))
    {
        ApplyOrderBy(t => t.Symbol);
    }
}

/// <summary>
/// Specification to get tokens with pagination.
/// </summary>
public sealed class TokensWithPaginationSpecification : BaseSpecification<Token>
{
    public TokensWithPaginationSpecification(string chainId, int pageNumber, int pageSize)
        : base(t => t.ChainId == chainId)
    {
        ApplyOrderByDescending(t => t.CreatedAt);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}
