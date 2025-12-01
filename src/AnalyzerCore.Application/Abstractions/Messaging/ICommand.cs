using AnalyzerCore.Domain.Abstractions;
using MediatR;

namespace AnalyzerCore.Application.Abstractions.Messaging;

/// <summary>
/// Represents a command that returns a Result (no value).
/// </summary>
public interface ICommand : IRequest<Result>, IBaseCommand
{
}

/// <summary>
/// Represents a command that returns a Result with a value.
/// </summary>
/// <typeparam name="TResponse">The type of the response value.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand
{
}

/// <summary>
/// Marker interface for commands.
/// </summary>
public interface IBaseCommand
{
}
