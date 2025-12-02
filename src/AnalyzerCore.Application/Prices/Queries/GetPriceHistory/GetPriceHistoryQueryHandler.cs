using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Prices.Queries.GetPriceHistory;

/// <summary>
/// Handler for GetPriceHistoryQuery.
/// </summary>
public sealed class GetPriceHistoryQueryHandler : IQueryHandler<GetPriceHistoryQuery, IReadOnlyList<TokenPrice>>
{
    private readonly IPriceService _priceService;

    public GetPriceHistoryQueryHandler(IPriceService priceService)
    {
        _priceService = priceService;
    }

    public async Task<Result<IReadOnlyList<TokenPrice>>> Handle(
        GetPriceHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var addressResult = EthereumAddress.Create(request.TokenAddress);
        if (addressResult.IsFailure)
            return Result.Failure<IReadOnlyList<TokenPrice>>(addressResult.Error);

        return await _priceService.GetPriceHistoryAsync(
            addressResult.Value.Value,
            request.QuoteCurrency,
            request.From,
            request.To,
            request.Limit,
            cancellationToken);
    }
}
