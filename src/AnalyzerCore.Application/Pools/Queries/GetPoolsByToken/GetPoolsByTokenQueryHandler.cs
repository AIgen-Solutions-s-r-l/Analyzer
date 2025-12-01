using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Pools.Queries.GetPoolsByToken;

/// <summary>
/// Handler for GetPoolsByTokenQuery.
/// </summary>
public sealed class GetPoolsByTokenQueryHandler : IQueryHandler<GetPoolsByTokenQuery, IReadOnlyList<Pool>>
{
    private readonly IPoolRepository _poolRepository;

    public GetPoolsByTokenQueryHandler(IPoolRepository poolRepository)
    {
        _poolRepository = poolRepository;
    }

    public async Task<Result<IReadOnlyList<Pool>>> Handle(
        GetPoolsByTokenQuery request,
        CancellationToken cancellationToken)
    {
        // Validate address
        var addressResult = EthereumAddress.Create(request.TokenAddress);
        if (addressResult.IsFailure)
            return Result.Failure<IReadOnlyList<Pool>>(addressResult.Error);

        var chainIdResult = ChainId.Create(request.ChainId);
        if (chainIdResult.IsFailure)
            return Result.Failure<IReadOnlyList<Pool>>(chainIdResult.Error);

        var pools = await _poolRepository.GetPoolsByTokenAsync(
            addressResult.Value.Value,
            chainIdResult.Value.Value,
            cancellationToken);

        return Result.Success<IReadOnlyList<Pool>>(pools.ToList());
    }
}
