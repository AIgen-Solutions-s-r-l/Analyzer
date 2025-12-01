using AnalyzerCore.Domain.Abstractions;
using MediatR;

namespace AnalyzerCore.Application.Abstractions.Messaging;

/// <summary>
/// Represents a query that returns a Result with a value.
/// Queries are read-only operations that don't change system state.
/// </summary>
/// <typeparam name="TResponse">The type of the response value.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
