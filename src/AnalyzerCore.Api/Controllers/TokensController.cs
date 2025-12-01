using AnalyzerCore.Api.Contracts.Tokens;
using AnalyzerCore.Application.Tokens.Commands.CreateToken;
using AnalyzerCore.Application.Tokens.Queries.GetTokenByAddress;
using AnalyzerCore.Application.Tokens.Queries.GetTokensByChainId;
using AnalyzerCore.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerCore.Api.Controllers;

/// <summary>
/// API endpoints for managing tokens.
/// </summary>
public class TokensController : ApiControllerBase
{
    private readonly ISender _sender;

    public TokensController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new token.
    /// </summary>
    /// <param name="request">The token creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created token.</returns>
    /// <response code="201">Token created successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="409">Token already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTokenRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTokenCommand
        {
            Address = request.Address,
            ChainId = request.ChainId,
            Symbol = request.Symbol ?? "UNKNOWN",
            Name = request.Name ?? "Unknown Token",
            Decimals = request.Decimals ?? 18,
            TotalSupply = 0
        };

        var result = await _sender.Send(command, cancellationToken);

        return result.Match(
            onSuccess: token => CreatedAtAction(
                nameof(GetByAddress),
                new { address = token.Address, chainId = token.ChainId },
                MapToResponse(token)),
            onFailure: error => ToActionResult(result));
    }

    /// <summary>
    /// Gets a token by address and chain ID.
    /// </summary>
    /// <param name="address">The token address.</param>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token if found.</returns>
    /// <response code="200">Token found.</response>
    /// <response code="404">Token not found.</response>
    [HttpGet("{address}")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAddress(
        [FromRoute] string address,
        [FromQuery] string chainId,
        CancellationToken cancellationToken)
    {
        var query = new GetTokenByAddressQuery(address, chainId);
        var result = await _sender.Send(query, cancellationToken);

        return result.Match(
            onSuccess: token => Ok(MapToResponse(token)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Gets all tokens for a specific chain.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tokens on the chain.</returns>
    /// <response code="200">Tokens retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TokenResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByChainId(
        [FromQuery] string chainId,
        CancellationToken cancellationToken)
    {
        var query = new GetTokensByChainIdQuery(chainId);
        var result = await _sender.Send(query, cancellationToken);

        return result.Match(
            onSuccess: tokens => Ok(tokens.Select(MapToResponse)),
            onFailure: _ => ToActionResult(result));
    }

    private static TokenResponse MapToResponse(Token token) => new()
    {
        Id = token.Id,
        Address = token.Address,
        ChainId = token.ChainId,
        Symbol = token.Symbol,
        Name = token.Name,
        Decimals = token.Decimals,
        IsPlaceholder = token.IsPlaceholder,
        CreatedAt = token.CreatedAt
    };
}
