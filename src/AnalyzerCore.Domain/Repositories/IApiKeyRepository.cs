using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Domain.Repositories;

/// <summary>
/// Repository interface for ApiKey aggregate.
/// </summary>
public interface IApiKeyRepository
{
    /// <summary>
    /// Gets an API Key by its unique identifier.
    /// </summary>
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all API Keys for a user.
    /// </summary>
    Task<IEnumerable<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an API Key by its prefix.
    /// </summary>
    Task<IEnumerable<ApiKey>> GetByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new API Key.
    /// </summary>
    Task<ApiKey> AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing API Key.
    /// </summary>
    Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an API Key.
    /// </summary>
    Task DeleteAsync(ApiKey apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
