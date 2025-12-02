using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Arbitrage.Queries.ScanArbitrage;

/// <summary>
/// Handler for ScanArbitrageQuery.
/// </summary>
public sealed class ScanArbitrageQueryHandler : IQueryHandler<ScanArbitrageQuery, IReadOnlyList<ArbitrageOpportunity>>
{
    private readonly IArbitrageService _arbitrageService;

    public ScanArbitrageQueryHandler(IArbitrageService arbitrageService)
    {
        _arbitrageService = arbitrageService;
    }

    public async Task<Result<IReadOnlyList<ArbitrageOpportunity>>> Handle(
        ScanArbitrageQuery request,
        CancellationToken cancellationToken)
    {
        return await _arbitrageService.ScanForOpportunitiesAsync(
            request.MinProfitUsd,
            cancellationToken);
    }
}
