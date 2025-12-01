using AnalyzerCore.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Web3;

namespace AnalyzerCore.Infrastructure.HealthChecks;

/// <summary>
/// Health check for blockchain RPC connectivity.
/// </summary>
public sealed class BlockchainRpcHealthCheck : IHealthCheck
{
    private readonly Web3 _web3;
    private readonly BlockchainOptions _options;
    private readonly ILogger<BlockchainRpcHealthCheck> _logger;

    public BlockchainRpcHealthCheck(
        Web3 web3,
        IOptions<BlockchainOptions> options,
        ILogger<BlockchainRpcHealthCheck> logger)
    {
        _web3 = web3;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10)); // 10 second timeout

            // Try to get the current block number
            var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();

            if (blockNumber == null || blockNumber.Value <= 0)
            {
                _logger.LogWarning("Blockchain RPC health check failed: Invalid block number");
                return HealthCheckResult.Unhealthy("Invalid block number returned from RPC");
            }

            // Try to get chain ID to verify we're connected to the right chain
            var chainId = await _web3.Eth.ChainId.SendRequestAsync();

            var data = new Dictionary<string, object>
            {
                { "chainId", chainId?.Value.ToString() ?? "unknown" },
                { "chainName", _options.Name },
                { "blockNumber", blockNumber.Value.ToString() },
                { "rpcUrl", MaskRpcUrl(_options.RpcUrl) }
            };

            // Verify chain ID matches expected
            if (chainId?.Value.ToString() != _options.ChainId)
            {
                _logger.LogWarning(
                    "Blockchain RPC health check warning: Chain ID mismatch. Expected {Expected}, got {Actual}",
                    _options.ChainId,
                    chainId?.Value.ToString());

                return HealthCheckResult.Degraded(
                    $"Chain ID mismatch. Expected {_options.ChainId}, got {chainId?.Value}",
                    data: data);
            }

            return HealthCheckResult.Healthy("Blockchain RPC is healthy", data);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Blockchain RPC health check timed out");
            return HealthCheckResult.Unhealthy("RPC request timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blockchain RPC health check failed with exception");
            return HealthCheckResult.Unhealthy("RPC check failed", ex);
        }
    }

    private static string MaskRpcUrl(string url)
    {
        // Mask any API keys in the URL
        if (string.IsNullOrEmpty(url))
            return "not configured";

        var uri = url.Contains("://") ? url : $"https://{url}";

        try
        {
            var parsed = new Uri(uri);
            return $"{parsed.Host}:{parsed.Port}";
        }
        catch
        {
            return "invalid url";
        }
    }
}
