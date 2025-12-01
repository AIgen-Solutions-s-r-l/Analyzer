using AnalyzerCore.Domain.Abstractions;
using FluentValidation;
using MediatR;

namespace AnalyzerCore.Application.Behaviors;

/// <summary>
/// Pipeline behavior that validates requests using FluentValidation.
/// Runs before the handler and short-circuits on validation failure.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .Select(f => f.ErrorMessage)
            .Distinct()
            .ToList();

        if (errors.Any())
        {
            return CreateValidationResult<TResponse>(errors);
        }

        return await next();
    }

    private static TResult CreateValidationResult<TResult>(List<string> errors)
        where TResult : Result
    {
        var validationError = new ValidationError(errors);

        // Handle Result<T>
        if (typeof(TResult).IsGenericType &&
            typeof(TResult).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure), 1, new[] { typeof(Error) })!
                .MakeGenericMethod(resultType);

            return (TResult)failureMethod.Invoke(null, new object[] { validationError })!;
        }

        // Handle Result (non-generic)
        return (TResult)(object)Result.Failure(validationError);
    }
}
