using AnalyzerCore.Api.Contracts.Pools;
using AnalyzerCore.Application.Pools.Commands.CreatePool;
using AnalyzerCore.Application.Pools.Commands.UpdatePoolReserves;
using AnalyzerCore.Application.Pools.Queries.GetPoolByAddress;
using AnalyzerCore.Application.Pools.Queries.GetPoolsByToken;
using AnalyzerCore.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalyzerCore.Api.Controllers;

/// <summary>
/// API endpoints for managing liquidity pools.
/// </summary>
[Authorize(Policy = "RequireReadOnly")]
public class PoolsController : ApiControllerBase
{
    private readonly ISender _sender;

    public PoolsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new liquidity pool.
    /// </summary>
    /// <param name="request">The pool creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created pool.</returns>
    /// <response code="201">Pool created successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="409">Pool already exists.</response>
    [HttpPost]
    [Authorize(Policy = "RequireUser")]
    [ProducesResponseType(typeof(PoolResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePoolRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePoolCommand
        {
            Address = request.Address,
            Token0Address = request.Token0Address,
            Token1Address = request.Token1Address,
            Factory = request.Factory,
            ChainId = request.ChainId
        };

        var result = await _sender.Send(command, cancellationToken);

        return result.Match(
            onSuccess: pool => CreatedAtAction(
                nameof(GetByAddress),
                new { address = pool.Address, factory = pool.Factory },
                MapToResponse(pool)),
            onFailure: error => ToActionResult(result));
    }

    /// <summary>
    /// Gets a pool by address and factory.
    /// </summary>
    /// <param name="address">The pool address.</param>
    /// <param name="factory">The factory address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The pool if found.</returns>
    /// <response code="200">Pool found.</response>
    /// <response code="404">Pool not found.</response>
    [HttpGet("{address}")]
    [ProducesResponseType(typeof(PoolResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAddress(
        [FromRoute] string address,
        [FromQuery] string factory,
        CancellationToken cancellationToken)
    {
        var query = new GetPoolByAddressQuery(address, factory);
        var result = await _sender.Send(query, cancellationToken);

        return result.Match(
            onSuccess: pool => Ok(MapToResponse(pool)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Gets all pools containing a specific token.
    /// </summary>
    /// <param name="tokenAddress">The token address.</param>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pools containing the token.</returns>
    /// <response code="200">Pools retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PoolResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByToken(
        [FromQuery] string tokenAddress,
        [FromQuery] string chainId,
        CancellationToken cancellationToken)
    {
        var query = new GetPoolsByTokenQuery(tokenAddress, chainId);
        var result = await _sender.Send(query, cancellationToken);

        return result.Match(
            onSuccess: pools => Ok(pools.Select(MapToResponse)),
            onFailure: _ => ToActionResult(result));
    }

    /// <summary>
    /// Updates the reserves of a pool.
    /// </summary>
    /// <param name="address">The pool address.</param>
    /// <param name="factory">The factory address.</param>
    /// <param name="request">The reserve update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Reserves updated successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="404">Pool not found.</response>
    [HttpPut("{address}/reserves")]
    [Authorize(Policy = "RequireUser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateReserves(
        [FromRoute] string address,
        [FromQuery] string factory,
        [FromBody] UpdatePoolReservesRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePoolReservesCommand
        {
            Address = address,
            Factory = factory,
            Reserve0 = request.Reserve0,
            Reserve1 = request.Reserve1
        };

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return ToActionResult(result);
    }

    private static PoolResponse MapToResponse(Pool pool) => new()
    {
        Id = pool.Id,
        Address = pool.Address,
        ChainId = pool.ChainId,
        Factory = pool.Factory,
        Token0Address = pool.Token0Address,
        Token1Address = pool.Token1Address,
        Reserve0 = pool.Reserve0,
        Reserve1 = pool.Reserve1,
        Type = pool.Type.ToString(),
        CreatedAt = pool.CreatedAt,
        UpdatedAt = pool.UpdatedAt
    };
}
