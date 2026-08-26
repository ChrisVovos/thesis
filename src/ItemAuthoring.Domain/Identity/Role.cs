using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Domain.Identity;

/// <summary>
/// A named bundle of permissions that can be assigned to users.
/// </summary>
public sealed class Role : AggregateRoot<RoleId>
{
    /// <summary>The inclusive maximum length of a role name.</summary>
    public const int MaxNameLength = 64;

    /// <summary>The inclusive maximum length of a role description.</summary>
    public const int MaxDescriptionLength = 256;

    private readonly List<RolePermission> _permissions = [];

    private Role(RoleId id, string name, string description, bool isSystemRole)
        : base(id)
    {
        Name = name;
        Description = description;
        IsSystemRole = isSystemRole;
    }

    private Role()
    {
    }

    /// <summary>Gets the role name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the human readable explanation of what the role is for.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether the role ships with the platform.</summary>
    /// <remarks>System roles may have their permissions adjusted but may not be renamed or deleted.</remarks>
    public bool IsSystemRole { get; private set; }

    /// <summary>Gets the permissions currently granted to the role.</summary>
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    /// <summary>Creates a role.</summary>
    /// <param name="name">The role name.</param>
    /// <param name="description">The human readable explanation.</param>
    /// <param name="isSystemRole">Whether the role ships with the platform.</param>
    /// <returns>The new role.</returns>
    /// <exception cref="DomainException">The name or description were invalid.</exception>
    public static Role Create(string? name, string? description, bool isSystemRole = false)
    {
        var trimmedName = Ensure.NotBlank(name, "role.name_required", "A role name is required.");
        Ensure.MaxLength(
            trimmedName,
            MaxNameLength,
            "role.name_too_long",
            $"A role name must not exceed {MaxNameLength} characters.");
        var trimmedDescription = Ensure.NotBlank(
            description,
            "role.description_required",
            "A role description is required.");
        Ensure.MaxLength(
            trimmedDescription,
            MaxDescriptionLength,
            "role.description_too_long",
            $"A role description must not exceed {MaxDescriptionLength} characters.");
        return new Role(RoleId.New(), trimmedName, trimmedDescription, isSystemRole);
    }

    /// <summary>Renames the role.</summary>
    /// <param name="name">The new role name.</param>
    /// <exception cref="DomainException">The role ships with the platform, or the name is invalid.</exception>
    public void Rename(string? name)
    {
        Ensure.That(!IsSystemRole, "role.system_immutable", "A system role cannot be renamed.");
        var trimmed = Ensure.NotBlank(name, "role.name_required", "A role name is required.");
        Name = Ensure.MaxLength(
            trimmed,
            MaxNameLength,
            "role.name_too_long",
            $"A role name must not exceed {MaxNameLength} characters.");
    }

    /// <summary>Replaces the description of the role.</summary>
    /// <param name="description">The new description.</param>
    /// <exception cref="DomainException">The description was invalid.</exception>
    public void Describe(string? description)
    {
        var trimmed = Ensure.NotBlank(
            description,
            "role.description_required",
            "A role description is required.");
        Description = Ensure.MaxLength(
            trimmed,
            MaxDescriptionLength,
            "role.description_too_long",
            $"A role description must not exceed {MaxDescriptionLength} characters.");
    }

    /// <summary>Grants a permission to the role, ignoring duplicates.</summary>
    /// <param name="permissionId">The permission to grant.</param>
    public void Grant(PermissionId permissionId)
    {
        if (_permissions.Exists(permission => permission.PermissionId == permissionId))
        {
            return;
        }

        _permissions.Add(RolePermission.Create(Id, permissionId));
    }

    /// <summary>Revokes a permission from the role.</summary>
    /// <param name="permissionId">The permission to revoke.</param>
    public void Revoke(PermissionId permissionId)
        => _permissions.RemoveAll(permission => permission.PermissionId == permissionId);

    /// <summary>Replaces the complete permission set of the role.</summary>
    /// <param name="permissionIds">The permissions the role should hold afterwards.</param>
    public void ReplacePermissions(IEnumerable<PermissionId> permissionIds)
    {
        _permissions.Clear();
        foreach (var permissionId in permissionIds.Distinct())
        {
            _permissions.Add(RolePermission.Create(Id, permissionId));
        }
    }
}
