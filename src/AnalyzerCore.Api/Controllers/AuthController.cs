using AnalyzerCore.Api.Contracts.Auth;
using AnalyzerCore.Domain.Entities;
using AnalyzerCore.Domain.Repositories;
using AnalyzerCore.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AnalyzerCore.Api.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        // Check if user already exists
        if (await _userRepository.ExistsAsync(request.Email, cancellationToken))
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
            return Conflict(new { message = "A user with this email already exists." });
        }

        // Hash password
        var passwordHash = _passwordHasher.Hash(request.Password);

        // Create user
        var userResult = User.Create(
            request.Email,
            request.DisplayName,
            passwordHash,
            UserRole.User);

        if (!userResult.IsSuccess)
        {
            return BadRequest(new { message = userResult.Error.Message });
        }

        var user = userResult.Value;

        // Generate tokens
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiration = _jwtTokenGenerator.GetRefreshTokenExpiration();

        // Store refresh token hash
        user.SetRefreshToken(
            _passwordHasher.Hash(refreshToken),
            refreshTokenExpiration);

        // Save user
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User registered: {Email}", user.Email);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString()
        };

        return CreatedAtAction(nameof(GetCurrentUser), null, response);
    }

    /// <summary>
    /// Login with email and password.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Login attempt with unknown email: {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for deactivated user: {Email}", request.Email);
            return Unauthorized(new { message = "User account is deactivated." });
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password for user: {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Generate tokens
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiration = _jwtTokenGenerator.GetRefreshTokenExpiration();

        // Update user with refresh token and login timestamp
        user.RecordLogin();
        user.SetRefreshToken(
            _passwordHasher.Hash(refreshToken),
            refreshTokenExpiration);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged in: {Email}", user.Email);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString()
        });
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        // Find user with valid refresh token
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var user = users.FirstOrDefault(u =>
            u.RefreshTokenHash != null &&
            u.RefreshTokenExpiresAt > DateTime.UtcNow &&
            _passwordHasher.Verify(request.RefreshToken, u.RefreshTokenHash));

        if (user is null)
        {
            _logger.LogWarning("Invalid refresh token attempt");
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Refresh token attempt for deactivated user: {Email}", user.Email);
            return Unauthorized(new { message = "User account is deactivated." });
        }

        // Generate new tokens
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiration = _jwtTokenGenerator.GetRefreshTokenExpiration();

        // Update refresh token
        user.SetRefreshToken(
            _passwordHasher.Hash(refreshToken),
            refreshTokenExpiration);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Token refreshed for user: {Email}", user.Email);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString()
        });
    }

    /// <summary>
    /// Logout current user (invalidate refresh token).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        user.ClearRefreshToken();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged out: {Email}", user.Email);

        return NoContent();
    }

    /// <summary>
    /// Get current authenticated user info.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new UserInfoResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        });
    }
}

/// <summary>
/// User information response.
/// </summary>
public sealed class UserInfoResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string Role { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}
