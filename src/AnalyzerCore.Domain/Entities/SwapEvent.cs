using AnalyzerCore.Domain.Abstractions;

namespace AnalyzerCore.Domain.Entities;

/// <summary>
/// Represents a swap event from a DEX pool.
/// Used for tracking trading volume and price impact.
/// </summary>
public class SwapEvent : Entity<long>
{
    private SwapEvent() { }

    /// <summary>
    /// The pool address where the swap occurred.
    /// </summary>
    public string PoolAddress { get; private set; } = null!;

    /// <summary>
    /// The chain ID where this swap occurred.
    /// </summary>
    public string ChainId { get; private set; } = null!;

    /// <summary>
    /// The transaction hash of the swap.
    /// </summary>
    public string TransactionHash { get; private set; } = null!;

    /// <summary>
    /// The block number where this swap was included.
    /// </summary>
    public long BlockNumber { get; private set; }

    /// <summary>
    /// The log index within the block.
    /// </summary>
    public int LogIndex { get; private set; }

    /// <summary>
    /// The address that initiated the swap.
    /// </summary>
    public string Sender { get; private set; } = null!;

    /// <summary>
    /// The address that received the output tokens.
    /// </summary>
    public string Recipient { get; private set; } = null!;

    /// <summary>
    /// Amount of token0 that went into the pool (positive) or came out (negative).
    /// </summary>
    public decimal Amount0 { get; private set; }

    /// <summary>
    /// Amount of token1 that went into the pool (positive) or came out (negative).
    /// </summary>
    public decimal Amount1 { get; private set; }

    /// <summary>
    /// The USD value of the swap at the time of execution.
    /// </summary>
    public decimal AmountUsd { get; private set; }

    /// <summary>
    /// Timestamp of the swap event.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Creates a new swap event.
    /// </summary>
    public static SwapEvent Create(
        string poolAddress,
        string chainId,
        string transactionHash,
        long blockNumber,
        int logIndex,
        string sender,
        string recipient,
        decimal amount0,
        decimal amount1,
        decimal amountUsd,
        DateTime timestamp)
    {
        return new SwapEvent
        {
            PoolAddress = poolAddress.ToLowerInvariant(),
            ChainId = chainId,
            TransactionHash = transactionHash.ToLowerInvariant(),
            BlockNumber = blockNumber,
            LogIndex = logIndex,
            Sender = sender.ToLowerInvariant(),
            Recipient = recipient.ToLowerInvariant(),
            Amount0 = amount0,
            Amount1 = amount1,
            AmountUsd = amountUsd,
            Timestamp = timestamp
        };
    }

    /// <summary>
    /// Gets the absolute trade volume (always positive).
    /// </summary>
    public decimal GetAbsoluteVolumeUsd() => Math.Abs(AmountUsd);
}
