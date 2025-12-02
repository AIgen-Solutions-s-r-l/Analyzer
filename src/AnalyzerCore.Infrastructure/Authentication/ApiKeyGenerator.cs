using System.Security.Cryptography;

namespace AnalyzerCore.Infrastructure.Authentication;

/// <summary>
/// API key generator implementation.
/// </summary>
public sealed class ApiKeyGenerator : IApiKeyGenerator
{
    private readonly IPasswordHasher _passwordHasher;

    public ApiKeyGenerator(IPasswordHasher passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public (string PlainTextKey, string KeyHash) Generate()
    {
        // Generate a secure random key
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        // Convert to base64url-safe string (no +, /, =)
        var plainTextKey = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        // Hash the key for storage
        var keyHash = _passwordHasher.Hash(plainTextKey);

        return (plainTextKey, keyHash);
    }
}
