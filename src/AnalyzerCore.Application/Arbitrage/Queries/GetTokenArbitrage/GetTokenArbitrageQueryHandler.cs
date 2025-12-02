using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Arbitrage.Queries.GetTokenArbitrage;

/// <summary>
/// Handler for GetTokenArbitrageQuery.
/// </summary>
public sealed class GetTokenArbitrageQueryHandler : IQueryHandler<GetTokenArbitrageQuery, IReadOnlyList<ArbitrageOpportunity>>
{
    private readonly IArbitrageService _arbitrageService;

    public GetTokenArbitrageQueryHandler(IArbitrageService arbitrageService)
    {
        _arbitrageService = arbitrageService;
    }

    public async Task<Result<IReadOnlyList<ArbitrageOpportunity>>> Handle(
        GetTokenArbitrageQuery request,
        CancellationToken cancellationToken)
    {
        var addressResult = EthereumAddress.Create(request.TokenAddress);
        if (addressResult.IsFailure)
            return Result.Failure<IReadOnlyList<ArbitrageOpportunity>>(addressResult.Error);

        return await _arbitrageService.FindOpportunitiesForTokenAsync(
            addressResult.Value.Value,
            cancellationToken);
    }
}
