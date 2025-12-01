using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Domain.Specifications;

/// <summary>
/// Specification to get a pool by address and factory.
/// </summary>
public sealed class PoolByAddressSpecification : BaseSpecification<Pool>
{
    public PoolByAddressSpecification(string address, string factory)
        : base(p => p.Address == address.ToLowerInvariant() && p.Factory == factory.ToLowerInvariant())
    {
        AddInclude(p => p.Token0);
        AddInclude(p => p.Token1);
    }
}

/// <summary>
/// Specification to get all pools by factory.
/// </summary>
public sealed class PoolsByFactorySpecification : BaseSpecification<Pool>
{
    public PoolsByFactorySpecification(string factory)
        : base(p => p.Factory == factory.ToLowerInvariant())
    {
        AddInclude(p => p.Token0);
        AddInclude(p => p.Token1);
        ApplyOrderByDescending(p => p.CreatedAt);
    }
}

/// <summary>
/// Specification to get pools by token address.
/// </summary>
public sealed class PoolsByTokenSpecification : BaseSpecification<Pool>
{
    public PoolsByTokenSpecification(string tokenAddress, string chainId)
        : base(p =>
            (p.Token0.Address == tokenAddress.ToLowerInvariant() ||
             p.Token1.Address == tokenAddress.ToLowerInvariant()) &&
            p.Token0.ChainId == chainId)
    {
        AddInclude(p => p.Token0);
        AddInclude(p => p.Token1);
        ApplyOrderByDescending(p => p.CreatedAt);
    }
}

/// <summary>
/// Specification to get pools created after a specific timestamp.
/// </summary>
public sealed class PoolsCreatedAfterSpecification : BaseSpecification<Pool>
{
    public PoolsCreatedAfterSpecification(DateTime timestamp, string factory)
        : base(p => p.CreatedAt >= timestamp && p.Factory == factory.ToLowerInvariant())
    {
        AddInclude(p => p.Token0);
        AddInclude(p => p.Token1);
        ApplyOrderByDescending(p => p.CreatedAt);
    }
}

/// <summary>
/// Specification to get pools with pagination.
/// </summary>
public sealed class PoolsWithPaginationSpecification : BaseSpecification<Pool>
{
    public PoolsWithPaginationSpecification(string chainId, int pageNumber, int pageSize)
        : base(p => p.Token0.ChainId == chainId)
    {
        AddInclude(p => p.Token0);
        AddInclude(p => p.Token1);
        ApplyOrderByDescending(p => p.CreatedAt);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}

/// <summary>
/// Specification to get pools with highest reserves.
/// </summary>
public sealed class TopPoolsByReservesSpecification : BaseSpecification<Pool>
{
    public TopPoolsByReservesSpecification(string chainId, int count)
        : base(p => p.Token0.ChainId == chainId && (p.Reserve0 > 0 || p.Reserve1 > 0))
    {
        AddInclude(p => p.Token0);
        AddInclude(p => p.Token1);
        ApplyOrderByDescending(p => p.Reserve0 + p.Reserve1);
        ApplyPaging(0, count);
    }
}
