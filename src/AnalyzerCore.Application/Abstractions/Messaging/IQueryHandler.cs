using AnalyzerCore.Domain.Abstractions;
using MediatR;

namespace AnalyzerCore.Application.Abstractions.Messaging;

/// <summary>
/// Handler for queries that return a Result with a value.
/// </summary>
/// <typeparam name="TQuery">The type of the query.</typeparam>
/// <typeparam name="TResponse">The type of the response value.</typeparam>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
