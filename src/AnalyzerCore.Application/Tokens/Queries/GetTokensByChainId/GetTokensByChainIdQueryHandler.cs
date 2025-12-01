using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.ValueObjects;

namespace AnalyzerCore.Application.Tokens.Queries.GetTokensByChainId;

/// <summary>
/// Handler for GetTokensByChainIdQuery.
/// </summary>
public sealed class GetTokensByChainIdQueryHandler : IQueryHandler<GetTokensByChainIdQuery, IReadOnlyList<Token>>
{
    private readonly ITokenRepository _tokenRepository;

    public GetTokensByChainIdQueryHandler(ITokenRepository tokenRepository)
    {
        _tokenRepository = tokenRepository;
    }

    public async Task<Result<IReadOnlyList<Token>>> Handle(
        GetTokensByChainIdQuery request,
        CancellationToken cancellationToken)
    {
        var chainIdResult = ChainId.Create(request.ChainId);
        if (chainIdResult.IsFailure)
            return Result.Failure<IReadOnlyList<Token>>(chainIdResult.Error);

        var tokens = await _tokenRepository.GetAllByChainIdAsync(
            chainIdResult.Value.Value,
            cancellationToken);

        return Result.Success<IReadOnlyList<Token>>(tokens.ToList());
    }
}
