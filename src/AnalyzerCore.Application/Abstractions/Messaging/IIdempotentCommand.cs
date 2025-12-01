using System;

namespace AnalyzerCore.Application.Abstractions.Messaging;

/// <summary>
/// Marker interface for idempotent commands.
/// Commands implementing this interface will be deduplicated using the RequestId.
/// </summary>
public interface IIdempotentCommand : ICommand
{
    /// <summary>
    /// Unique identifier for this command request.
    /// Used to detect and prevent duplicate processing.
    /// </summary>
    Guid RequestId { get; }
}

/// <summary>
/// Marker interface for idempotent commands that return a result.
/// Commands implementing this interface will be deduplicated using the RequestId.
/// </summary>
public interface IIdempotentCommand<TResponse> : ICommand<TResponse>
{
    /// <summary>
    /// Unique identifier for this command request.
    /// Used to detect and prevent duplicate processing.
    /// </summary>
    Guid RequestId { get; }
}
