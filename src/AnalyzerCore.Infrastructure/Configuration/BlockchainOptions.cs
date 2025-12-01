using System.ComponentModel.DataAnnotations;

namespace AnalyzerCore.Infrastructure.Configuration;

/// <summary>
/// Configuration options for blockchain connection.
/// </summary>
public sealed class BlockchainOptions
{
    public const string SectionName = "ChainConfig";

    /// <summary>
    /// The chain ID (e.g., "1" for Ethereum mainnet).
    /// </summary>
    [Required]
    public string ChainId { get; set; } = "1";

    /// <summary>
    /// Human-readable name of the chain.
    /// </summary>
    [Required]
    public string Name { get; set; } = "Ethereum";

    /// <summary>
    /// RPC endpoint URL (without protocol prefix).
    /// </summary>
    [Required]
    public string RpcUrl { get; set; } = string.Empty;

    /// <summary>
    /// RPC port number.
    /// </summary>
    [Range(1, 65535)]
    public int RpcPort { get; set; } = 443;

    /// <summary>
    /// Average block time in seconds.
    /// </summary>
    [Range(1, 600)]
    public int BlockTime { get; set; } = 12;

    /// <summary>
    /// Number of blocks to wait for confirmation.
    /// </summary>
    [Range(0, 100)]
    public int ConfirmationBlocks { get; set; } = 12;

    /// <summary>
    /// Gets the full RPC URL with protocol.
    /// </summary>
    public string GetFullRpcUrl()
    {
        if (RpcUrl.StartsWith("http://") || RpcUrl.StartsWith("https://"))
            return RpcUrl;

        return $"https://{RpcUrl}";
    }
}
