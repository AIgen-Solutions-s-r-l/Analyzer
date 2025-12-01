using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.Pools.Commands.UpdatePoolReserves;

/// <summary>
/// Handler for UpdatePoolReservesCommand.
/// </summary>
public sealed class UpdatePoolReservesCommandHandler : ICommandHandler<UpdatePoolReservesCommand>
{
    private readonly IPoolRepository _poolRepository;
    private readonly ILogger<UpdatePoolReservesCommandHandler> _logger;

    public UpdatePoolReservesCommandHandler(
        IPoolRepository poolRepository,
        ILogger<UpdatePoolReservesCommandHandler> logger)
    {
        _poolRepository = poolRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdatePoolReservesCommand request, CancellationToken cancellationToken)
    {
        // Parse value objects
        var addressResult = EthereumAddress.Create(request.Address);
        if (addressResult.IsFailure)
            return Result.Failure(addressResult.Error);

        var factoryResult = EthereumAddress.Create(request.Factory);
        if (factoryResult.IsFailure)
            return Result.Failure(factoryResult.Error);

        var poolAddress = addressResult.Value;
        var factory = factoryResult.Value;

        // Get the pool
        var pool = await _poolRepository.GetByAddressAsync(
            poolAddress.Value,
            factory.Value,
            cancellationToken);

        if (pool is null)
        {
            _logger.LogWarning(
                "Pool {Address} not found on factory {Factory}",
                poolAddress,
                factory);
            return Result.Failure(DomainErrors.Pool.NotFound(poolAddress.Value));
        }

        // Update reserves using domain method
        var updateResult = pool.UpdateReserves(request.Reserve0, request.Reserve1);
        if (updateResult.IsFailure)
            return updateResult;

        _logger.LogInformation(
            "Updated reserves for pool {Address} ({PairName}): {Reserve0}/{Reserve1}",
            pool.Address,
            pool.GetPairName(),
            request.Reserve0,
            request.Reserve1);

        // Note: SaveChanges is handled by UnitOfWorkBehavior

        return Result.Success();
    }
}
