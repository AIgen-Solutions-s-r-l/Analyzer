using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Services;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Prices.Queries.GetTokenPrice;

/// <summary>
/// Handler for GetTokenPriceQuery.
/// </summary>
public sealed class GetTokenPriceQueryHandler : IQueryHandler<GetTokenPriceQuery, TokenPrice>
{
    private readonly IPriceService _priceService;

    public GetTokenPriceQueryHandler(IPriceService priceService)
    {
        _priceService = priceService;
    }

    public async Task<Result<TokenPrice>> Handle(
        GetTokenPriceQuery request,
        CancellationToken cancellationToken)
    {
        var addressResult = EthereumAddress.Create(request.TokenAddress);
        if (addressResult.IsFailure)
            return Result.Failure<TokenPrice>(addressResult.Error);

        return await _priceService.GetTokenPriceAsync(
            addressResult.Value.Value,
            request.QuoteCurrency,
            cancellationToken);
    }
}
