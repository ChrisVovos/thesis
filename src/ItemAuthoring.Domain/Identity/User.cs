using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Identity.Events;

namespace ItemAuthoring.Domain.Identity;

/// <summary>
/// A person who signs in to the authoring platform.
/// </summary>
public sealed partial class User : AggregateRoot<UserId>
{
    private readonly List<UserRole> _roles = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private User(UserId id, EmailAddress email, DisplayName displayName, PasswordHash passwordHash)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        IsActive = true;
    }

    private User()
    {
    }

    /// <summary>Gets the login identifier of the user.</summary>
    public EmailAddress Email { get; private set; } = null!;

    /// <summary>Gets the human readable name shown in the interface.</summary>
    public DisplayName DisplayName { get; private set; } = null!;

    /// <summary>Gets the stored password hash.</summary>
    public PasswordHash PasswordHash { get; private set; } = null!;

    /// <summary>Gets a value indicating whether the user may sign in.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets the number of consecutive failed sign-in attempts.</summary>
    public int FailedSignInAttempts { get; private set; }

    /// <summary>Gets the instant, in UTC, until which sign-in is blocked.</summary>
    public DateTimeOffset? LockedOutUntilUtc { get; private set; }

    /// <summary>Gets the instant, in UTC, of the most recent successful sign-in.</summary>
    public DateTimeOffset? LastSignInAtUtc { get; private set; }

    /// <summary>Gets the roles assigned to the user.</summary>
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    /// <summary>Gets the refresh tokens issued to the user.</summary>
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    /// <summary>Creates an active user.</summary>
    /// <param name="email">The login identifier.</param>
    /// <param name="displayName">The human readable name.</param>
    /// <param name="passwordHash">The already computed password hash.</param>
    /// <returns>The new user.</returns>
    public static User Create(EmailAddress email, DisplayName displayName, PasswordHash passwordHash)
    {
        var user = new User(UserId.New(), email, displayName, passwordHash);
        user.Raise(new UserCreatedDomainEvent(user.Id, email.Value));
        return user;
    }

    /// <summary>Replaces the profile details of the user.</summary>
    /// <param name="email">The new login identifier.</param>
    /// <param name="displayName">The new human readable name.</param>
    public void UpdateProfile(EmailAddress email, DisplayName displayName)
    {
        Email = email;
        DisplayName = displayName;
    }

    /// <summary>Replaces the stored password hash and revokes every outstanding refresh token.</summary>
    /// <param name="passwordHash">The newly computed password hash.</param>
    /// <param name="atUtc">The instant at which the change takes effect.</param>
    public void ChangePassword(PasswordHash passwordHash, DateTimeOffset atUtc)
    {
        PasswordHash = passwordHash;
        FailedSignInAttempts = 0;
        LockedOutUntilUtc = null;
        RevokeAllRefreshTokens(atUtc);
        Raise(new UserPasswordChangedDomainEvent(Id));
    }

    /// <summary>Allows the user to sign in again.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Prevents the user from signing in and revokes every outstanding refresh token.</summary>
    /// <param name="atUtc">The instant at which the change takes effect.</param>
    public void Deactivate(DateTimeOffset atUtc)
    {
        IsActive = false;
        RevokeAllRefreshTokens(atUtc);
    }

    /// <summary>Assigns a role to the user, ignoring duplicates.</summary>
    /// <param name="roleId">The role to assign.</param>
    public void AssignRole(RoleId roleId)
    {
        if (_roles.Exists(role => role.RoleId == roleId))
        {
            return;
        }

        _roles.Add(UserRole.Create(Id, roleId));
    }

    /// <summary>Removes a role from the user.</summary>
    /// <param name="roleId">The role to remove.</param>
    public void RemoveRole(RoleId roleId) => _roles.RemoveAll(role => role.RoleId == roleId);

    /// <summary>Replaces the complete role assignment of the user.</summary>
    /// <param name="roleIds">The roles the user should hold afterwards.</param>
    /// <exception cref="DomainException">No role was supplied.</exception>
    public void ReplaceRoles(IEnumerable<RoleId> roleIds)
    {
        var distinct = roleIds.Distinct().ToList();
        Ensure.That(
            distinct.Count > 0,
            "user.roles_required",
            "A user must hold at least one role.");

        _roles.Clear();
        foreach (var roleId in distinct)
        {
            _roles.Add(UserRole.Create(Id, roleId));
        }
    }
}
