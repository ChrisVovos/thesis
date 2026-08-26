namespace ItemAuthoring.Domain.Identity;

/// <summary>
/// The assignment of a <see cref="Role"/> to a <see cref="User"/>.
/// </summary>
public sealed class UserRole
{
    private UserRole(UserId userId, RoleId roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    private UserRole()
    {
    }

    /// <summary>Gets the user the role is assigned to.</summary>
    public UserId UserId { get; private set; }

    /// <summary>Gets the assigned role.</summary>
    public RoleId RoleId { get; private set; }

    /// <summary>Creates an assignment.</summary>
    /// <param name="userId">The user the role is assigned to.</param>
    /// <param name="roleId">The assigned role.</param>
    /// <returns>The assignment.</returns>
    internal static UserRole Create(UserId userId, RoleId roleId) => new(userId, roleId);
}
