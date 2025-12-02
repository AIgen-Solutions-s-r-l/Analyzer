using AnalyzerCore.Domain.Abstractions;
using AnalyzerCore.Domain.Entities;

namespace AnalyzerCore.Domain.Events;

/// <summary>
/// Raised when a new user is created.
/// </summary>
public sealed record UserCreatedDomainEvent(
    Guid UserId,
    string Email,
    UserRole Role) : DomainEvent;

/// <summary>
/// Raised when a user's role is changed.
/// </summary>
public sealed record UserRoleChangedDomainEvent(
    Guid UserId,
    UserRole OldRole,
    UserRole NewRole) : DomainEvent;
