using System.Numerics;
using AnalyzerCore.Application.Pools.Commands.CreatePool;
using AnalyzerCore.Domain.Models;
using AnalyzerCore.Domain.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace AnalyzerCore.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that monitors the blockchain for new pools.
/// </summary>
public class BlockchainMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlockchainMonitorService> _logger;
    private readonly ChainConfig _chainConfig;
    private readonly int _pollingInterval;
    private readonly int _blocksToProcess;
    private readonly int _batchSize;
    private readonly int _retryDelay;
    private readonly int _maxRetries;
    private readonly int _requestDelay;
    private readonly AsyncRetryPolicy _retryPolicy;

    public BlockchainMonitorService(
        IServiceProvider serviceProvider,
        ChainConfig chainConfig,
        ILogger<BlockchainMonitorService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _chainConfig = chainConfig;
        _logger = logger;

        var monitoring = configuration.GetSection("Monitoring");
        _pollingInterval = monitoring.GetValue<int>("PollingInterval");
        _blocksToProcess = monitoring.GetValue<int>("BlocksToProcess");
        _batchSize = monitoring.GetValue<int>("BatchSize");
        _retryDelay = monitoring.GetValue<int>("RetryDelay");
        _maxRetries = monitoring.GetValue<int>("MaxRetries");
        _requestDelay = monitoring.GetValue<int>("RequestDelay");

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                _maxRetries,
                retryAttempt => TimeSpan.FromMilliseconds(_retryDelay * Math.Pow(2, retryAttempt - 1)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Error connecting to blockchain (Attempt {RetryCount} of {MaxRetries}). Retrying in {Delay}ms...",
                        retryCount,
                        _maxRetries,
                        timeSpan.TotalMilliseconds);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Starting blockchain monitor for chain {ChainName} (ID: {ChainId})",
            _chainConfig.Name,
            _chainConfig.ChainId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBlocksAsync(stoppingToken);
                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                _logger.LogInformation("Blockchain monitor is stopping...");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing blocks on chain {ChainName}. Service will continue...",
                    _chainConfig.Name);

                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessBlocksAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var currentBlock = await blockchainService.GetCurrentBlockNumberAsync(stoppingToken);
        var fromBlock = currentBlock - BigInteger.Parse(_blocksToProcess.ToString());
        var toBlock = currentBlock;

        _logger.LogInformation(
            "Processing blocks {FromBlock} to {ToBlock} on chain {ChainName}",
            fromBlock,
            toBlock,
            _chainConfig.Name);

        // Process blocks in batches
        for (var batchStart = fromBlock;
             batchStart <= toBlock && !stoppingToken.IsCancellationRequested;
             batchStart += _batchSize)
        {
            var batchEnd = BigInteger.Min(batchStart + _batchSize - 1, toBlock);

            await _retryPolicy.ExecuteAsync(async () =>
            {
                var blocks = await blockchainService.GetBlocksAsync(batchStart, batchEnd, stoppingToken);
                await ProcessBlockBatchAsync(blocks, blockchainService, mediator, stoppingToken);
            });

            // Add delay between batches to respect rate limits
            if (batchEnd < toBlock)
            {
                await Task.Delay(_requestDelay, stoppingToken);
            }
        }
    }

    private async Task ProcessBlockBatchAsync(
        IEnumerable<BlockData> blocks,
        IBlockchainService blockchainService,
        IMediator mediator,
        CancellationToken stoppingToken)
    {
        foreach (var block in blocks)
        {
            if (stoppingToken.IsCancellationRequested) break;

            foreach (var tx in block.Transactions)
            {
                if (stoppingToken.IsCancellationRequested) break;

                if (string.IsNullOrEmpty(tx.To))
                {
                    continue; // Skip contract creation transactions
                }

                await TryProcessPoolAsync(tx.To, blockchainService, mediator, stoppingToken);
            }
        }
    }

    private async Task TryProcessPoolAsync(
        string contractAddress,
        IBlockchainService blockchainService,
        IMediator mediator,
        CancellationToken stoppingToken)
    {
        try
        {
            if (!await blockchainService.IsContractAsync(contractAddress, stoppingToken))
            {
                return; // Skip non-contract addresses
            }

            var poolInfo = await blockchainService.GetPoolInfoAsync(contractAddress, stoppingToken);
            if (poolInfo is null ||
                string.IsNullOrEmpty(poolInfo.Token0) ||
                string.IsNullOrEmpty(poolInfo.Token1) ||
                string.IsNullOrEmpty(poolInfo.Factory))
            {
                return; // Skip invalid pool info
            }

            _logger.LogInformation(
                "Found pool at {Address} with tokens {Token0}/{Token1}",
                contractAddress,
                poolInfo.Token0,
                poolInfo.Token1);

            var command = new CreatePoolCommand
            {
                Address = contractAddress,
                Token0Address = poolInfo.Token0,
                Token1Address = poolInfo.Token1,
                Factory = poolInfo.Factory,
                Type = poolInfo.Type,
                ChainId = _chainConfig.ChainId
            };

            var result = await mediator.Send(command, stoppingToken);

            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to create pool {Address}: {Error}",
                    contractAddress,
                    result.Error.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(
                ex,
                "Contract {Address} is not a pool",
                contractAddress);
        }
    }
}
