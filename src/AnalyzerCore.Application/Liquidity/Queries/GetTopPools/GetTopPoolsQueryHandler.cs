using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Liquidity.Queries.GetTopPools;

/// <summary>
/// Handler for GetTopPoolsQuery.
/// </summary>
public sealed class GetTopPoolsQueryHandler : IQueryHandler<GetTopPoolsQuery, IReadOnlyList<LiquidityMetrics>>
{
    private readonly ILiquidityAnalyticsService _liquidityService;

    public GetTopPoolsQueryHandler(ILiquidityAnalyticsService liquidityService)
    {
        _liquidityService = liquidityService;
    }

    public async Task<Result<IReadOnlyList<LiquidityMetrics>>> Handle(
        GetTopPoolsQuery request,
        CancellationToken cancellationToken)
    {
        return await _liquidityService.GetTopPoolsByTvlAsync(
            Math.Min(request.Limit, 100),
            cancellationToken);
    }
}
