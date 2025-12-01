using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Events;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Domain.Entities;

/// <summary>
/// Represents a liquidity pool (e.g., Uniswap V2 pair).
/// </summary>
public class Pool : AggregateRoot<int>
{
    // Private constructor for EF Core
    private Pool() { }

    /// <summary>
    /// The pool's contract address.
    /// </summary>
    public string Address { get; private set; } = null!;

    /// <summary>
    /// The first token in the pair.
    /// </summary>
    public Token Token0 { get; private set; } = null!;

    /// <summary>
    /// The ID of Token0 (for EF Core navigation).
    /// </summary>
    public int Token0Id { get; private set; }

    /// <summary>
    /// The second token in the pair.
    /// </summary>
    public Token Token1 { get; private set; } = null!;

    /// <summary>
    /// The ID of Token1 (for EF Core navigation).
    /// </summary>
    public int Token1Id { get; private set; }

    /// <summary>
    /// The factory contract that created this pool.
    /// </summary>
    public string Factory { get; private set; } = null!;

    /// <summary>
    /// The reserve amount of Token0.
    /// </summary>
    public decimal Reserve0 { get; private set; }

    /// <summary>
    /// The reserve amount of Token1.
    /// </summary>
    public decimal Reserve1 { get; private set; }

    /// <summary>
    /// When this pool was first discovered.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the reserves were last updated.
    /// </summary>
    public DateTime LastUpdated { get; private set; }

    /// <summary>
    /// The type of pool (e.g., UniswapV2, UniswapV3).
    /// </summary>
    public PoolType Type { get; private set; } = PoolType.UniswapV2;

    /// <summary>
    /// Creates a new Pool entity with validated parameters.
    /// </summary>
    public static Result<Pool> Create(
        EthereumAddress address,
        Token token0,
        Token token1,
        EthereumAddress factory,
        PoolType type = PoolType.UniswapV2)
    {
        // Validate tokens are different
        if (token0.Address == token1.Address)
            return Result.Failure<Pool>(DomainErrors.Pool.InvalidTokenPair);

        var pool = new Pool
        {
            Address = address.Value,
            Token0 = token0,
            Token0Id = token0.Id,
            Token1 = token1,
            Token1Id = token1.Id,
            Factory = factory.Value,
            Type = type,
            Reserve0 = 0,
            Reserve1 = 0,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        pool.RaiseDomainEvent(new PoolCreatedDomainEvent(
            pool.Address,
            token0.Address,
            token1.Address,
            pool.Factory,
            pool.Type));

        return Result.Success(pool);
    }

    /// <summary>
    /// Legacy factory method for backward compatibility.
    /// </summary>
    [Obsolete("Use Create(EthereumAddress, ...) instead")]
    public static Pool Create(
        string address,
        Token token0,
        Token token1,
        string factory,
        PoolType type = PoolType.UniswapV2)
    {
        return new Pool
        {
            Address = address.ToLowerInvariant(),
            Token0 = token0,
            Token1 = token1,
            Factory = factory.ToLowerInvariant(),
            Type = type,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the pool reserves.
    /// </summary>
    public Result UpdateReserves(decimal reserve0, decimal reserve1)
    {
        if (reserve0 < 0 || reserve1 < 0)
            return Result.Failure(DomainErrors.Pool.InvalidReserves);

        var previousReserve0 = Reserve0;
        var previousReserve1 = Reserve1;

        Reserve0 = reserve0;
        Reserve1 = reserve1;
        LastUpdated = DateTime.UtcNow;

        // Only raise event if reserves actually changed
        if (previousReserve0 != reserve0 || previousReserve1 != reserve1)
        {
            RaiseDomainEvent(new PoolReservesUpdatedDomainEvent(
                Address,
                previousReserve0,
                previousReserve1,
                reserve0,
                reserve1));
        }

        return Result.Success();
    }

    /// <summary>
    /// Gets the current price of Token0 in terms of Token1.
    /// </summary>
    public decimal? GetToken0Price()
    {
        if (Reserve1 == 0) return null;
        return Reserve0 / Reserve1;
    }

    /// <summary>
    /// Gets the current price of Token1 in terms of Token0.
    /// </summary>
    public decimal? GetToken1Price()
    {
        if (Reserve0 == 0) return null;
        return Reserve1 / Reserve0;
    }

    /// <summary>
    /// Gets the total value locked (TVL) if both tokens have known prices.
    /// </summary>
    public decimal GetTotalValueLocked(decimal token0PriceUsd, decimal token1PriceUsd)
    {
        return (Reserve0 * token0PriceUsd) + (Reserve1 * token1PriceUsd);
    }

    /// <summary>
    /// Gets the EthereumAddress value object for this pool.
    /// </summary>
    public EthereumAddress GetAddress() => EthereumAddress.FromTrusted(Address);

    /// <summary>
    /// Gets the factory's EthereumAddress value object.
    /// </summary>
    public EthereumAddress GetFactory() => EthereumAddress.FromTrusted(Factory);

    /// <summary>
    /// Returns a string representation of the pool (token pair).
    /// </summary>
    public string GetPairName() => $"{Token0?.Symbol ?? "?"}/{Token1?.Symbol ?? "?"}";
}
