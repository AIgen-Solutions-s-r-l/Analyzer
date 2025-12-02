using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Liquidity.Queries.GetTokenLiquidity;

/// <summary>
/// Handler for GetTokenLiquidityQuery.
/// </summary>
public sealed class GetTokenLiquidityQueryHandler : IQueryHandler<GetTokenLiquidityQuery, TokenLiquiditySummary>
{
    private readonly ILiquidityAnalyticsService _liquidityService;

    public GetTokenLiquidityQueryHandler(ILiquidityAnalyticsService liquidityService)
    {
        _liquidityService = liquidityService;
    }

    public async Task<Result<TokenLiquiditySummary>> Handle(
        GetTokenLiquidityQuery request,
        CancellationToken cancellationToken)
    {
        var addressResult = EthereumAddress.Create(request.TokenAddress);
        if (addressResult.IsFailure)
            return Result.Failure<TokenLiquiditySummary>(addressResult.Error);

        return await _liquidityService.GetTokenLiquiditySummaryAsync(
            addressResult.Value.Value,
            cancellationToken);
    }
}
