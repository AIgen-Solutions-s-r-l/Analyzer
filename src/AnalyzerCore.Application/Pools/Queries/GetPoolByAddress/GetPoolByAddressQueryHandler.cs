using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Pools.Queries.GetPoolByAddress;

/// <summary>
/// Handler for GetPoolByAddressQuery.
/// </summary>
public sealed class GetPoolByAddressQueryHandler : IQueryHandler<GetPoolByAddressQuery, Pool>
{
    private readonly IPoolRepository _poolRepository;

    public GetPoolByAddressQueryHandler(IPoolRepository poolRepository)
    {
        _poolRepository = poolRepository;
    }

    public async Task<Result<Pool>> Handle(GetPoolByAddressQuery request, CancellationToken cancellationToken)
    {
        // Validate address
        var addressResult = EthereumAddress.Create(request.Address);
        if (addressResult.IsFailure)
            return Result.Failure<Pool>(addressResult.Error);

        var factoryResult = EthereumAddress.Create(request.Factory);
        if (factoryResult.IsFailure)
            return Result.Failure<Pool>(factoryResult.Error);

        var pool = await _poolRepository.GetByAddressAsync(
            addressResult.Value.Value,
            factoryResult.Value.Value,
            cancellationToken);

        if (pool is null)
            return Result.Failure<Pool>(DomainErrors.Pool.NotFound(request.Address));

        return Result.Success(pool);
    }
}
