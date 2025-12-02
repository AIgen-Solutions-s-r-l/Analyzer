using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Prices.Queries.GetTwap;

/// <summary>
/// Handler for GetTwapQuery.
/// </summary>
public sealed class GetTwapQueryHandler : IQueryHandler<GetTwapQuery, TwapResult>
{
    private readonly IPriceService _priceService;

    public GetTwapQueryHandler(IPriceService priceService)
    {
        _priceService = priceService;
    }

    public async Task<Result<TwapResult>> Handle(
        GetTwapQuery request,
        CancellationToken cancellationToken)
    {
        var addressResult = EthereumAddress.Create(request.TokenAddress);
        if (addressResult.IsFailure)
            return Result.Failure<TwapResult>(addressResult.Error);

        var period = TimeSpan.FromMinutes(request.PeriodMinutes);

        return await _priceService.GetTwapAsync(
            addressResult.Value.Value,
            request.QuoteCurrency,
            period,
            cancellationToken);
    }
}
