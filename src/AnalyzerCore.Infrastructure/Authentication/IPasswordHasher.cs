namespace AnalyzerCore.Infrastructure.Authentication;

/// <summary>
/// Service for hashing and verifying passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using BCrypt.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    bool Verify(string password, string hash);
}
