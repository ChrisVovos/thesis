using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Identity.Events;

/// <summary>Raised when a user account has been created.</summary>
/// <param name="UserId">The identity of the new user.</param>
/// <param name="Email">The login identifier of the new user.</param>
public sealed record UserCreatedDomainEvent(UserId UserId, string Email) : DomainEvent;

/// <summary>Raised when a user's password has been replaced.</summary>
/// <param name="UserId">The identity of the user.</param>
public sealed record UserPasswordChangedDomainEvent(UserId UserId) : DomainEvent;
