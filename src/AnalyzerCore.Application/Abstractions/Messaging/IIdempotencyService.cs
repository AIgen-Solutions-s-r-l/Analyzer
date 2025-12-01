using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnalyzerCore.Application.Abstractions.Messaging;

/// <summary>
/// Service for managing idempotent request tracking.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Checks if a request with the given ID has already been processed.
    /// </summary>
    Task<bool> RequestExistsAsync(Guid requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a record of the request being processed.
    /// </summary>
    Task CreateRequestAsync(Guid requestId, string name, CancellationToken cancellationToken = default);
}
