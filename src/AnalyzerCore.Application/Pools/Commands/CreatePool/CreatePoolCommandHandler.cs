using AnalyzerCore.Application.Abstractions.Messaging;
using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Errors;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AnalyzerCore.Application.Pools.Commands.CreatePool;

/// <summary>
/// Handler for CreatePoolCommand.
/// </summary>
public sealed class CreatePoolCommandHandler : ICommandHandler<CreatePoolCommand, Pool>
{
    private readonly IPoolRepository _poolRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly ILogger<CreatePoolCommandHandler> _logger;

    public CreatePoolCommandHandler(
        IPoolRepository poolRepository,
        ITokenRepository tokenRepository,
        ILogger<CreatePoolCommandHandler> logger)
    {
        _poolRepository = poolRepository;
        _tokenRepository = tokenRepository;
        _logger = logger;
    }

    public async Task<Result<Pool>> Handle(CreatePoolCommand request, CancellationToken cancellationToken)
    {
        // Parse value objects
        var addressResult = EthereumAddress.Create(request.Address);
        if (addressResult.IsFailure)
            return Result.Failure<Pool>(addressResult.Error);

        var factoryResult = EthereumAddress.Create(request.Factory);
        if (factoryResult.IsFailure)
            return Result.Failure<Pool>(factoryResult.Error);

        var chainIdResult = ChainId.Create(request.ChainId);
        if (chainIdResult.IsFailure)
            return Result.Failure<Pool>(chainIdResult.Error);

        var poolAddress = addressResult.Value;
        var factory = factoryResult.Value;
        var chainId = chainIdResult.Value;

        // Check if pool already exists
        if (await _poolRepository.ExistsAsync(poolAddress.Value, factory.Value, cancellationToken))
        {
            _logger.LogInformation(
                "Pool {Address} already exists on factory {Factory}",
                poolAddress,
                factory);

            var existingPool = await _poolRepository.GetByAddressAsync(
                poolAddress.Value,
                factory.Value,
                cancellationToken);

            return existingPool is not null
                ? Result.Success(existingPool)
                : Result.Failure<Pool>(DomainErrors.Pool.NotFound(poolAddress.Value));
        }

        // Get or create tokens
        var token0Result = await GetOrCreateTokenAsync(request.Token0Address, chainId, cancellationToken);
        if (token0Result.IsFailure)
            return Result.Failure<Pool>(token0Result.Error);

        var token1Result = await GetOrCreateTokenAsync(request.Token1Address, chainId, cancellationToken);
        if (token1Result.IsFailure)
            return Result.Failure<Pool>(token1Result.Error);

        // Create pool
        var poolResult = Pool.Create(
            poolAddress,
            token0Result.Value,
            token1Result.Value,
            factory,
            request.Type);

        if (poolResult.IsFailure)
            return poolResult;

        var pool = poolResult.Value;

        _logger.LogInformation(
            "Creating pool {Address} ({PairName}) on factory {Factory}",
            pool.Address,
            pool.GetPairName(),
            pool.Factory);

        await _poolRepository.AddAsync(pool, cancellationToken);

        // Note: SaveChanges is handled by UnitOfWorkBehavior

        return Result.Success(pool);
    }

    private async Task<Result<Token>> GetOrCreateTokenAsync(
        string address,
        ChainId chainId,
        CancellationToken cancellationToken)
    {
        var addressResult = EthereumAddress.Create(address);
        if (addressResult.IsFailure)
            return Result.Failure<Token>(addressResult.Error);

        var tokenAddress = addressResult.Value;

        // Try to get existing token
        var existingToken = await _tokenRepository.GetByAddressAsync(
            tokenAddress.Value,
            chainId.Value,
            cancellationToken);

        if (existingToken is not null)
            return Result.Success(existingToken);

        // Create placeholder token
        _logger.LogInformation(
            "Token {Address} not found on chain {ChainId}, creating placeholder",
            tokenAddress,
            chainId);

        var tokenResult = Token.CreatePlaceholder(tokenAddress, chainId);
        if (tokenResult.IsFailure)
            return tokenResult;

        var token = tokenResult.Value;
        await _tokenRepository.AddAsync(token, cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(token);
    }
}
