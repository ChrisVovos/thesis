using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Abstractions.Persistence;

/// <summary>
/// Loads and stores <see cref="User"/> aggregates.
/// </summary>
public interface IUserRepository
{
    /// <summary>Loads a user together with the roles assigned to them.</summary>
    /// <param name="userId">The user to load.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The user, or <see langword="null"/> when they do not exist.</returns>
    Task<User?> GetAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>Loads a user by login identifier, including their refresh tokens.</summary>
    /// <param name="normalizedEmail">The upper-cased login identifier.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The user, or <see langword="null"/> when they do not exist.</returns>
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    /// <summary>Loads a user by the hash of a refresh token they hold.</summary>
    /// <param name="tokenHash">The Base64 encoded SHA-256 hash of the presented token.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The user, or <see langword="null"/> when the token is unknown.</returns>
    Task<User?> GetByRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Determines whether a login identifier is already taken.</summary>
    /// <param name="normalizedEmail">The upper-cased login identifier.</param>
    /// <param name="excluding">A user to ignore, used when changing an address.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> when the address is taken.</returns>
    Task<bool> EmailExistsAsync(
        string normalizedEmail,
        UserId? excluding = null,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the role names and permissions currently held by a user.</summary>
    /// <param name="userId">The user to inspect.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The role names and the union of their permissions.</returns>
    Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> GetAuthorizationDataAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    /// <summary>Registers a new user for insertion.</summary>
    /// <param name="user">The user to add.</param>
    void Add(User user);
}

/// <summary>
/// Loads and stores <see cref="Role"/> aggregates.
/// </summary>
public interface IRoleRepository
{
    /// <summary>Loads a role together with its permission grants.</summary>
    /// <param name="roleId">The role to load.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The role, or <see langword="null"/> when it does not exist.</returns>
    Task<Role?> GetAsync(RoleId roleId, CancellationToken cancellationToken = default);

    /// <summary>Loads a role by name.</summary>
    /// <param name="name">The role name.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The role, or <see langword="null"/> when it does not exist.</returns>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Determines which of the supplied identifiers do not exist.</summary>
    /// <param name="roleIds">The identifiers to verify.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identifiers that were not found.</returns>
    Task<IReadOnlyList<RoleId>> FindMissingAsync(
        IReadOnlyCollection<RoleId> roleIds,
        CancellationToken cancellationToken = default);

    /// <summary>Determines whether a role name is already taken.</summary>
    /// <param name="name">The candidate name.</param>
    /// <param name="excluding">A role to ignore, used when renaming.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> when the name is taken.</returns>
    Task<bool> NameExistsAsync(
        string name,
        RoleId? excluding = null,
        CancellationToken cancellationToken = default);

    /// <summary>Determines whether any user currently holds a role.</summary>
    /// <param name="roleId">The role to test.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns><see langword="true"/> when the role is assigned to at least one user.</returns>
    Task<bool> IsAssignedAsync(RoleId roleId, CancellationToken cancellationToken = default);

    /// <summary>Registers a new role for insertion.</summary>
    /// <param name="role">The role to add.</param>
    void Add(Role role);

    /// <summary>Registers a role for deletion.</summary>
    /// <param name="role">The role to remove.</param>
    void Remove(Role role);
}

/// <summary>
/// Reads the permission catalogue.
/// </summary>
public interface IPermissionRepository
{
    /// <summary>Loads every permission known to the application.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The permission catalogue.</returns>
    Task<IReadOnlyList<Permission>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads the permissions whose names appear in the supplied set.</summary>
    /// <param name="names">The permission names to look up.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The matching permissions.</returns>
    Task<IReadOnlyList<Permission>> GetByNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken = default);
}
