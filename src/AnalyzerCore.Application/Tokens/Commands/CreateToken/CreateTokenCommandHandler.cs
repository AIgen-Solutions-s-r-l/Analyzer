using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.Tokens.Commands.CreateToken;

/// <summary>
/// Handler for CreateTokenCommand.
/// </summary>
public sealed class CreateTokenCommandHandler : ICommandHandler<CreateTokenCommand, Token>
{
    private readonly ITokenRepository _tokenRepository;
    private readonly ILogger<CreateTokenCommandHandler> _logger;

    public CreateTokenCommandHandler(
        ITokenRepository tokenRepository,
        ILogger<CreateTokenCommandHandler> logger)
    {
        _tokenRepository = tokenRepository;
        _logger = logger;
    }

    public async Task<Result<Token>> Handle(CreateTokenCommand request, CancellationToken cancellationToken)
    {
        // Parse value objects
        var addressResult = EthereumAddress.Create(request.Address);
        if (addressResult.IsFailure)
            return Result.Failure<Token>(addressResult.Error);

        var chainIdResult = ChainId.Create(request.ChainId);
        if (chainIdResult.IsFailure)
            return Result.Failure<Token>(chainIdResult.Error);

        var tokenAddress = addressResult.Value;
        var chainId = chainIdResult.Value;

        // Check if token already exists
        var existingToken = await _tokenRepository.GetByAddressAsync(
            tokenAddress.Value,
            chainId.Value,
            cancellationToken);

        if (existingToken is not null)
        {
            _logger.LogInformation(
                "Token {Symbol} ({Address}) already exists on chain {ChainId}",
                existingToken.Symbol,
                tokenAddress,
                chainId);

            return Result.Success(existingToken);
        }

        // Create new token
        var tokenResult = Token.Create(
            tokenAddress,
            request.Symbol,
            request.Name,
            request.Decimals,
            request.TotalSupply,
            chainId);

        if (tokenResult.IsFailure)
            return tokenResult;

        var token = tokenResult.Value;

        _logger.LogInformation(
            "Creating token {Symbol} ({Address}) on chain {ChainId}",
            token.Symbol,
            token.Address,
            token.ChainId);

        await _tokenRepository.AddAsync(token, cancellationToken);

        // Note: SaveChanges is handled by UnitOfWorkBehavior

        return Result.Success(token);
    }
}
