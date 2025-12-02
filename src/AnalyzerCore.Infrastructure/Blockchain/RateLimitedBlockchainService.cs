using System.Numerics;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Models;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using AnalyzerCore.Infrastructure.RateLimiting;
using AnalyzerCore.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Infrastructure.Blockchain;

/// <summary>
/// Decorator that adds rate limiting to blockchain service calls.
/// </summary>
public sealed class RateLimitedBlockchainService : IBlockchainService
{
    private readonly IBlockchainService _inner;
    private readonly IRpcRateLimiter _rateLimiter;
    private readonly ILogger<RateLimitedBlockchainService> _logger;
    private readonly ApplicationMetrics? _metrics;

    public RateLimitedBlockchainService(
        IBlockchainService inner,
        IRpcRateLimiter rateLimiter,
        ILogger<RateLimitedBlockchainService> logger,
        ApplicationMetrics? metrics = null)
    {
        _inner = inner;
        _rateLimiter = rateLimiter;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<BigInteger> GetCurrentBlockNumberAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.GetCurrentBlockNumberAsync(cancellationToken),
            "eth_blockNumber",
            cancellationToken);
    }

    public async Task<IEnumerable<BlockData>> GetBlocksAsync(
        BigInteger fromBlock,
        BigInteger toBlock,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.GetBlocksAsync(fromBlock, toBlock, cancellationToken),
            "eth_getBlockByNumber",
            cancellationToken);
    }

    public async Task<TokenInfo> GetTokenInfoAsync(string address, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.GetTokenInfoAsync(address, cancellationToken),
            "eth_call_tokenInfo",
            cancellationToken);
    }

    public async Task<PoolInfo> GetPoolInfoAsync(string address, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.GetPoolInfoAsync(address, cancellationToken),
            "eth_call_poolInfo",
            cancellationToken);
    }

    public async Task<(decimal Reserve0, decimal Reserve1)> GetPoolReservesAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.GetPoolReservesAsync(address, cancellationToken),
            "eth_call_getReserves",
            cancellationToken);
    }

    public async Task<bool> IsContractAsync(string address, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.IsContractAsync(address, cancellationToken),
            "eth_getCode",
            cancellationToken);
    }

    public async Task<string> GetContractCreatorAsync(string address, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.GetContractCreatorAsync(address, cancellationToken),
            "eth_getTransactionByHash",
            cancellationToken);
    }

    public async Task<IEnumerable<TransactionInfo>> GetTransactionsByAddressAsync(
        string address,
        BigInteger fromBlock,
        BigInteger toBlock,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.GetTransactionsByAddressAsync(address, fromBlock, toBlock, cancellationToken),
            "eth_getLogs",
            cancellationToken);
    }

    public async Task<decimal> GetTokenBalanceAsync(
        string tokenAddress,
        string walletAddress,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.GetTokenBalanceAsync(tokenAddress, walletAddress, cancellationToken),
            "eth_call_balanceOf",
            cancellationToken);
    }

    public async Task<Result<BigInteger>> GetCurrentBlockNumberSafeAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.GetCurrentBlockNumberSafeAsync(cancellationToken),
            "eth_blockNumber",
            cancellationToken);
    }

    public async Task<Result<TokenInfo>> GetTokenInfoSafeAsync(
        EthereumAddress address,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.GetTokenInfoSafeAsync(address, cancellationToken),
            "eth_call_tokenInfo",
            cancellationToken);
    }

    public async Task<Result<PoolInfo>> GetPoolInfoSafeAsync(
        EthereumAddress address,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.GetPoolInfoSafeAsync(address, cancellationToken),
            "eth_call_poolInfo",
            cancellationToken);
    }

    public async Task<Result<bool>> IsContractSafeAsync(
        EthereumAddress address,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRateLimitAsync(
            () => _inner.IsContractSafeAsync(address, cancellationToken),
            "eth_getCode",
            cancellationToken);
    }

    private async Task<T> ExecuteWithRateLimitAsync<T>(
        Func<Task<T>> operation,
        string methodName,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        var acquired = await _rateLimiter.AcquireAsync(cancellationToken);
        if (!acquired)
        {
            _logger.LogWarning("Rate limit exceeded for RPC method {Method}", methodName);
            throw new RateLimitExceededException($"Rate limit exceeded for {methodName}");
        }

        try
        {
            _metrics?.RecordRpcCall(methodName);
            var result = await operation();

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            _metrics?.RecordRpcDuration(duration, methodName);

            return result;
        }
        catch (Exception ex) when (ex is not RateLimitExceededException)
        {
            _metrics?.RecordRpcError(methodName);
            throw;
        }
        finally
        {
            if (_rateLimiter is SlidingWindowRateLimiter slidingWindow)
            {
                slidingWindow.Release();
            }
        }
    }
}

/// <summary>
/// Exception thrown when rate limit is exceeded.
/// </summary>
public sealed class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message) : base(message) { }
}
