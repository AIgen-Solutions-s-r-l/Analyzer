using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Tokens.Queries.GetTokenByAddress;

/// <summary>
/// Handler for GetTokenByAddressQuery.
/// </summary>
public sealed class GetTokenByAddressQueryHandler : IQueryHandler<GetTokenByAddressQuery, Token>
{
    private readonly ITokenRepository _tokenRepository;

    public GetTokenByAddressQueryHandler(ITokenRepository tokenRepository)
    {
        _tokenRepository = tokenRepository;
    }

    public async Task<Result<Token>> Handle(GetTokenByAddressQuery request, CancellationToken cancellationToken)
    {
        // Validate address
        var addressResult = EthereumAddress.Create(request.Address);
        if (addressResult.IsFailure)
            return Result.Failure<Token>(addressResult.Error);

        var chainIdResult = ChainId.Create(request.ChainId);
        if (chainIdResult.IsFailure)
            return Result.Failure<Token>(chainIdResult.Error);

        var token = await _tokenRepository.GetByAddressAsync(
            addressResult.Value.Value,
            chainIdResult.Value.Value,
            cancellationToken);

        if (token is null)
            return Result.Failure<Token>(DomainErrors.Token.NotFound(request.Address));

        return Result.Success(token);
    }
}
