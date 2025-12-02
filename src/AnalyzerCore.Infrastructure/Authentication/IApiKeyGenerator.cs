namespace AnalyzerCore.Infrastructure.Authentication;

/// <summary>
/// Service for generating API keys.
/// </summary>
public interface IApiKeyGenerator
{
    /// <summary>
    /// Generates a new API key.
    /// </summary>
    /// <returns>The plain text key and its hash.</returns>
    (string PlainTextKey, string KeyHash) Generate();
}
