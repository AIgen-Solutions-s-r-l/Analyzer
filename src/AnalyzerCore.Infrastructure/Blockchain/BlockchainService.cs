using System.Diagnostics;
using System.Numerics;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Models;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;
using AnalyzerCore.Infrastructure.Configuration;
using AnalyzerCore.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace AnalyzerCore.Infrastructure.Blockchain;

/// <summary>
/// Service for interacting with blockchain via RPC.
/// </summary>
public class BlockchainService : IBlockchainService
{
    private readonly Web3 _web3;
    private readonly ILogger<BlockchainService> _logger;
    private readonly BlockchainOptions _options;

    public BlockchainService(
        Web3 web3,
        IOptions<BlockchainOptions> options,
        ILogger<BlockchainService> logger)
    {
        _web3 = web3;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BigInteger> GetCurrentBlockNumberAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.Blockchain.StartActivity("GetCurrentBlockNumber");
        activity?.SetRpcTags(method: "eth_blockNumber", endpoint: _options.GetFullRpcUrl());

        try
        {
            var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            activity?.SetBlockchainTags(chainId: _options.ChainId.ToString(), blockNumber: blockNumber.Value.ToString());
            activity?.SetSuccess();
            return blockNumber.Value;
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }
    }

    public async Task<IEnumerable<BlockData>> GetBlocksAsync(
        BigInteger fromBlock,
        BigInteger toBlock,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.Blockchain.StartActivity("GetBlocks");
        activity?.SetTag("blockchain.from_block", fromBlock.ToString());
        activity?.SetTag("blockchain.to_block", toBlock.ToString());
        activity?.SetTag("blockchain.block_count", (toBlock - fromBlock + 1).ToString());
        activity?.SetRpcTags(method: "eth_getBlockByNumber", endpoint: _options.GetFullRpcUrl());

        try
        {
            var blocks = new List<BlockData>();
            var batchSize = 10;

            for (var i = fromBlock; i <= toBlock; i += batchSize)
            {
                var tasks = new List<Task<BlockWithTransactions>>();
                var end = BigInteger.Min(i + batchSize - 1, toBlock);

                for (var blockNumber = i; blockNumber <= end; blockNumber++)
                {
                    var task = _web3.Eth.Blocks.GetBlockWithTransactionsByNumber
                        .SendRequestAsync(new HexBigInteger(blockNumber));
                    tasks.Add(task);
                }

                var results = await Task.WhenAll(tasks);

                foreach (var block in results.Where(b => b != null))
                {
                    blocks.Add(MapToBlockData(block));
                }
            }

            activity?.SetTag("blockchain.blocks_retrieved", blocks.Count);
            activity?.SetSuccess();
            return blocks;
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }
    }

    public async Task<TokenInfo> GetTokenInfoAsync(string address, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.Blockchain.StartActivity("GetTokenInfo");
        activity?.SetTokenTags(tokenAddress: address);
        activity?.SetRpcTags(method: "eth_call", endpoint: _options.GetFullRpcUrl());

        try
        {
            var contract = _web3.Eth.GetContract(ERC20ABI.ABI, address);

            var nameTask = contract.GetFunction("name").CallAsync<string>();
            var symbolTask = contract.GetFunction("symbol").CallAsync<string>();
            var decimalsTask = contract.GetFunction("decimals").CallAsync<int>();
            var totalSupplyTask = contract.GetFunction("totalSupply").CallAsync<BigInteger>();

            await Task.WhenAll(nameTask, symbolTask, decimalsTask, totalSupplyTask);

            var tokenInfo = new TokenInfo
            {
                Address = address.ToLowerInvariant(),
                Name = await nameTask,
                Symbol = await symbolTask,
                Decimals = await decimalsTask,
                TotalSupply = Web3.Convert.FromWei(await totalSupplyTask)
            };

            activity?.SetTokenTags(symbol: tokenInfo.Symbol, decimals: tokenInfo.Decimals);
            activity?.SetSuccess();
            return tokenInfo;
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }
    }

    public async Task<PoolInfo> GetPoolInfoAsync(string address, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.Blockchain.StartActivity("GetPoolInfo");
        activity?.SetPoolTags(poolAddress: address);
        activity?.SetRpcTags(method: "eth_call", endpoint: _options.GetFullRpcUrl());

        try
        {
            var contract = _web3.Eth.GetContract(UniswapV2PairABI.ABI, address);

            var token0Task = contract.GetFunction("token0").CallAsync<string>();
            var token1Task = contract.GetFunction("token1").CallAsync<string>();
            var factoryTask = contract.GetFunction("factory").CallAsync<string>();
            var reservesTask = contract.GetFunction("getReserves").CallAsync<Reserves>();

            await Task.WhenAll(token0Task, token1Task, factoryTask, reservesTask);
            var reserves = await reservesTask;

            var poolInfo = new PoolInfo
            {
                Address = address.ToLowerInvariant(),
                Token0 = (await token0Task)?.ToLowerInvariant() ?? string.Empty,
                Token1 = (await token1Task)?.ToLowerInvariant() ?? string.Empty,
                Factory = (await factoryTask)?.ToLowerInvariant() ?? string.Empty,
                Reserve0 = Web3.Convert.FromWei(reserves.Reserve0),
                Reserve1 = Web3.Convert.FromWei(reserves.Reserve1),
                Type = PoolType.UniswapV2
            };

            activity?.SetPoolTags(token0: poolInfo.Token0, token1: poolInfo.Token1);
            activity?.SetTag("pool.reserve0", poolInfo.Reserve0.ToString("F6"));
            activity?.SetTag("pool.reserve1", poolInfo.Reserve1.ToString("F6"));
            activity?.SetSuccess();
            return poolInfo;
        }
        catch (Exception ex)
        {
            activity?.RecordException(ex);
            throw;
        }
    }

    public async Task<(decimal Reserve0, decimal Reserve1)> GetPoolReservesAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        var contract = _web3.Eth.GetContract(UniswapV2PairABI.ABI, address);
        var reserves = await contract.GetFunction("getReserves").CallAsync<Reserves>();

        return (
            Web3.Convert.FromWei(reserves.Reserve0),
            Web3.Convert.FromWei(reserves.Reserve1)
        );
    }

    public async Task<bool> IsContractAsync(string address, CancellationToken cancellationToken = default)
    {
        var code = await _web3.Eth.GetCode.SendRequestAsync(address);
        return !string.IsNullOrEmpty(code) && code != "0x";
    }

    public async Task<string> GetContractCreatorAsync(string address, CancellationToken cancellationToken = default)
    {
        var transaction = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(address);
        return transaction?.From ?? string.Empty;
    }

    public async Task<IEnumerable<TransactionInfo>> GetTransactionsByAddressAsync(
        string address,
        BigInteger fromBlock,
        BigInteger toBlock,
        CancellationToken cancellationToken = default)
    {
        var filterInput = new NewFilterInput
        {
            FromBlock = new BlockParameter(new HexBigInteger(fromBlock)),
            ToBlock = new BlockParameter(new HexBigInteger(toBlock)),
            Address = new[] { address }
        };

        var logs = await _web3.Eth.Filters.GetLogs.SendRequestAsync(filterInput);
        var transactions = new List<TransactionInfo>();

        foreach (var log in logs)
        {
            var transaction = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(log.TransactionHash);
            if (transaction != null)
            {
                var block = await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber
                    .SendRequestAsync(log.BlockNumber);

                transactions.Add(new TransactionInfo
                {
                    Hash = transaction.TransactionHash,
                    From = transaction.From,
                    To = transaction.To,
                    Value = transaction.Value.Value,
                    Input = transaction.Input,
                    GasUsed = transaction.Gas.Value,
                    Status = true,
                    Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)block.Timestamp.Value).UtcDateTime
                });
            }
        }

        return transactions;
    }

    public async Task<decimal> GetTokenBalanceAsync(
        string tokenAddress,
        string walletAddress,
        CancellationToken cancellationToken = default)
    {
        var contract = _web3.Eth.GetContract(ERC20ABI.ABI, tokenAddress);
        var balance = await contract.GetFunction("balanceOf")
            .CallAsync<BigInteger>(walletAddress);

        return Web3.Convert.FromWei(balance);
    }

    #region Result-based methods (new)

    /// <summary>
    /// Gets the current block number with Result pattern.
    /// </summary>
    public async Task<Result<BigInteger>> GetCurrentBlockNumberSafeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            return Result.Success(blockNumber.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current block number");
            return Result.Failure<BigInteger>(DomainErrors.Blockchain.RpcError("eth_blockNumber", ex.Message));
        }
    }

    /// <summary>
    /// Gets token info with Result pattern.
    /// </summary>
    public async Task<Result<TokenInfo>> GetTokenInfoSafeAsync(
        EthereumAddress address,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenInfo = await GetTokenInfoAsync(address.Value, cancellationToken);
            return Result.Success(tokenInfo);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get token info for {Address}", address);
            return Result.Failure<TokenInfo>(
                DomainErrors.Blockchain.ContractCallFailed(address.Value, "ERC20 methods"));
        }
    }

    /// <summary>
    /// Gets pool info with Result pattern.
    /// </summary>
    public async Task<Result<PoolInfo>> GetPoolInfoSafeAsync(
        EthereumAddress address,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var poolInfo = await GetPoolInfoAsync(address.Value, cancellationToken);

            if (string.IsNullOrEmpty(poolInfo.Token0) ||
                string.IsNullOrEmpty(poolInfo.Token1) ||
                string.IsNullOrEmpty(poolInfo.Factory))
            {
                return Result.Failure<PoolInfo>(
                    DomainErrors.Blockchain.ContractCallFailed(address.Value, "Invalid pool data"));
            }

            return Result.Success(poolInfo);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get pool info for {Address}", address);
            return Result.Failure<PoolInfo>(
                DomainErrors.Blockchain.ContractCallFailed(address.Value, "UniswapV2Pair methods"));
        }
    }

    /// <summary>
    /// Checks if address is a contract with Result pattern.
    /// </summary>
    public async Task<Result<bool>> IsContractSafeAsync(
        EthereumAddress address,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isContract = await IsContractAsync(address.Value, cancellationToken);
            return Result.Success(isContract);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if {Address} is a contract", address);
            return Result.Failure<bool>(DomainErrors.Blockchain.RpcError("eth_getCode", ex.Message));
        }
    }

    #endregion

    #region Private methods

    private static BlockData MapToBlockData(BlockWithTransactions block)
    {
        return new BlockData
        {
            Number = block.Number.Value,
            Hash = block.BlockHash,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)block.Timestamp.Value).UtcDateTime,
            Transactions = block.Transactions.Select(tx => new TransactionInfo
            {
                Hash = tx.TransactionHash,
                From = tx.From,
                To = tx.To,
                Value = tx.Value.Value,
                Input = tx.Input,
                GasUsed = tx.Gas.Value,
                Status = true,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)block.Timestamp.Value).UtcDateTime
            })
        };
    }

    [FunctionOutput]
    private class Reserves
    {
        [Parameter("uint112", "_reserve0", 1)]
        public BigInteger Reserve0 { get; set; }

        [Parameter("uint112", "_reserve1", 2)]
        public BigInteger Reserve1 { get; set; }

        [Parameter("uint32", "_blockTimestampLast", 3)]
        public uint BlockTimestampLast { get; set; }
    }

    #endregion
}
