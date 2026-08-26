namespace ItemAuthoring.Domain.Identity;

/// <summary>
/// The grant of a single <see cref="Permission"/> to a single <see cref="Role"/>.
/// </summary>
public sealed class RolePermission
{
    private RolePermission(RoleId roleId, PermissionId permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    private RolePermission()
    {
    }

    /// <summary>Gets the role the permission is granted to.</summary>
    public RoleId RoleId { get; private set; }

    /// <summary>Gets the granted permission.</summary>
    public PermissionId PermissionId { get; private set; }

    /// <summary>Creates a grant.</summary>
    /// <param name="roleId">The role the permission is granted to.</param>
    /// <param name="permissionId">The granted permission.</param>
    /// <returns>The grant.</returns>
    internal static RolePermission Create(RoleId roleId, PermissionId permissionId)
        => new(roleId, permissionId);
}
