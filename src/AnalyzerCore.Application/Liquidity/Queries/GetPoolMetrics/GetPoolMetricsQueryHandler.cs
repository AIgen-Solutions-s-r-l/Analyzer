using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Liquidity.Queries.GetPoolMetrics;

/// <summary>
/// Handler for GetPoolMetricsQuery.
/// </summary>
public sealed class GetPoolMetricsQueryHandler : IQueryHandler<GetPoolMetricsQuery, LiquidityMetrics>
{
    private readonly ILiquidityAnalyticsService _liquidityService;

    public GetPoolMetricsQueryHandler(ILiquidityAnalyticsService liquidityService)
    {
        _liquidityService = liquidityService;
    }

    public async Task<Result<LiquidityMetrics>> Handle(
        GetPoolMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var addressResult = EthereumAddress.Create(request.PoolAddress);
        if (addressResult.IsFailure)
            return Result.Failure<LiquidityMetrics>(addressResult.Error);

        return await _liquidityService.GetPoolMetricsAsync(
            addressResult.Value.Value,
            cancellationToken);
    }
}
