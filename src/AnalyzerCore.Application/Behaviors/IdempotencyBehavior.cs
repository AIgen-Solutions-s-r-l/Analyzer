using System;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.Behaviors;

/// <summary>
/// Pipeline behavior that ensures idempotent command processing.
/// Prevents duplicate processing of the same command request.
/// </summary>
public sealed class IdempotencyBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand<TResponse>
    where TResponse : Result
{
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;

    public IdempotencyBehavior(
        IIdempotencyService idempotencyService,
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    {
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TRequest).Name;

        if (await _idempotencyService.RequestExistsAsync(request.RequestId, cancellationToken))
        {
            _logger.LogWarning(
                "Duplicate request detected. RequestId: {RequestId}, Command: {CommandName}",
                request.RequestId,
                commandName);

            // Return success for idempotent duplicate requests
            return CreateSuccessResult();
        }

        await _idempotencyService.CreateRequestAsync(request.RequestId, commandName, cancellationToken);

        _logger.LogDebug(
            "Processing idempotent command. RequestId: {RequestId}, Command: {CommandName}",
            request.RequestId,
            commandName);

        return await next();
    }

    private static TResponse CreateSuccessResult()
    {
        // Create appropriate success Result based on TResponse type
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Success();
        }

        // For Result<T>, we need to use reflection to create the appropriate success
        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var defaultValue = valueType.IsValueType ? Activator.CreateInstance(valueType) : null;

            var successMethod = typeof(Result).GetMethod(nameof(Result.Success), new[] { valueType });
            if (successMethod is not null)
            {
                return (TResponse)successMethod.Invoke(null, new[] { defaultValue })!;
            }
        }

        throw new InvalidOperationException($"Cannot create success result for type {typeof(TResponse)}");
    }
}
