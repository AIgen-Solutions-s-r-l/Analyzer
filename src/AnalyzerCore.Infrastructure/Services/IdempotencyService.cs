using System;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AnalyzerCore.Infrastructure.Services;

/// <summary>
/// Service for managing idempotent request tracking using EF Core.
/// </summary>
public sealed class IdempotencyService : IIdempotencyService
{
    private readonly ApplicationDbContext _dbContext;

    public IdempotencyService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> RequestExistsAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IdempotentRequests
            .AnyAsync(r => r.Id == requestId, cancellationToken);
    }

    public async Task CreateRequestAsync(Guid requestId, string name, CancellationToken cancellationToken = default)
    {
        var request = new IdempotentRequest(requestId, name, DateTime.UtcNow);
        _dbContext.IdempotentRequests.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
