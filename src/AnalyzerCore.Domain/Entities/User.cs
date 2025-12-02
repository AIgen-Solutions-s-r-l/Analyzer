using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Events;

namespace AnalyzerCore.Domain.Entities;

/// <summary>
/// Represents a system user with authentication credentials.
/// </summary>
public class User : AggregateRoot<Guid>
{
    // Private constructor for EF Core
    private User() { }

    /// <summary>
    /// The user's email address (used for login).
    /// </summary>
    public string Email { get; private set; } = null!;

    /// <summary>
    /// The user's display name.
    /// </summary>
    public string DisplayName { get; private set; } = null!;

    /// <summary>
    /// The hashed password.
    /// </summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>
    /// The user's role for authorization.
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// When this user was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the user last logged in.
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// The current refresh token hash (if any).
    /// </summary>
    public string? RefreshTokenHash { get; private set; }

    /// <summary>
    /// When the refresh token expires.
    /// </summary>
    public DateTime? RefreshTokenExpiresAt { get; private set; }

    /// <summary>
    /// Creates a new User entity with validated parameters.
    /// </summary>
    public static Result<User> Create(
        string email,
        string displayName,
        string passwordHash,
        UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<User>(Error.Validation("User.InvalidEmail", "Email is required."));

        if (!email.Contains('@'))
            return Result.Failure<User>(Error.Validation("User.InvalidEmail", "Email format is invalid."));

        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Failure<User>(Error.Validation("User.InvalidDisplayName", "Display name is required."));

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<User>(Error.Validation("User.InvalidPassword", "Password hash is required."));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant().Trim(),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        user.RaiseDomainEvent(new UserCreatedDomainEvent(user.Id, user.Email, user.Role));

        return Result.Success(user);
    }

    /// <summary>
    /// Records a successful login.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the refresh token.
    /// </summary>
    public void SetRefreshToken(string tokenHash, DateTime expiresAt)
    {
        RefreshTokenHash = tokenHash;
        RefreshTokenExpiresAt = expiresAt;
    }

    /// <summary>
    /// Clears the refresh token (logout).
    /// </summary>
    public void ClearRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiresAt = null;
    }

    /// <summary>
    /// Deactivates the user account.
    /// </summary>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(Error.Validation("User.AlreadyDeactivated", "User is already deactivated."));

        IsActive = false;
        ClearRefreshToken();

        return Result.Success();
    }

    /// <summary>
    /// Reactivates the user account.
    /// </summary>
    public Result Activate()
    {
        if (IsActive)
            return Result.Failure(Error.Validation("User.AlreadyActive", "User is already active."));

        IsActive = true;

        return Result.Success();
    }

    /// <summary>
    /// Changes the user's role.
    /// </summary>
    public void ChangeRole(UserRole newRole)
    {
        var oldRole = Role;
        Role = newRole;

        RaiseDomainEvent(new UserRoleChangedDomainEvent(Id, oldRole, newRole));
    }

    /// <summary>
    /// Updates the password hash.
    /// </summary>
    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        ClearRefreshToken(); // Force re-login after password change
    }
}

/// <summary>
/// User roles for authorization.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Read-only access to data.
    /// </summary>
    ReadOnly = 0,

    /// <summary>
    /// Standard user with read/write access.
    /// </summary>
    User = 1,

    /// <summary>
    /// Administrator with full access.
    /// </summary>
    Admin = 2
}
