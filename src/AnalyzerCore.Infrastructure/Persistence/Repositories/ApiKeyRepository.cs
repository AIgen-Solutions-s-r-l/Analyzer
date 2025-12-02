using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IApiKeyRepository.
/// </summary>
public class ApiKeyRepository : IApiKeyRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApiKeyRepository> _logger;

    public ApiKeyRepository(
        ApplicationDbContext context,
        ILogger<ApiKeyRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ApiKey>> GetByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .Where(k => k.KeyPrefix == prefix && k.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiKey> AddAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Adding API Key {Name} for user {UserId}",
            apiKey.Name,
            apiKey.UserId);

        var entry = await _context.ApiKeys.AddAsync(apiKey, cancellationToken);
        return entry.Entity;
    }

    public Task UpdateAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        _context.ApiKeys.Update(apiKey);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        _context.ApiKeys.Remove(apiKey);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error saving API key changes to database");
            throw;
        }
    }
}
