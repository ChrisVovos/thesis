using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace ItemAuthoring.Infrastructure.Persistence.Repositories;

/// <summary>The Entity Framework Core implementation of <see cref="IRoleRepository"/>.</summary>
/// <param name="context">The Entity Framework Core session.</param>
internal sealed class RoleRepository(ApplicationDbContext context) : IRoleRepository
{
    /// <inheritdoc />
    public Task<Role?> GetAsync(RoleId roleId, CancellationToken cancellationToken = default)
        => context.Roles
            .Include(role => role.Permissions)
            .FirstOrDefaultAsync(role => role.Id == roleId, cancellationToken);

    /// <inheritdoc />
    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => context.Roles
            .Include(role => role.Permissions)
            .FirstOrDefaultAsync(role => role.Name == name, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleId>> FindMissingAsync(
        IReadOnlyCollection<RoleId> roleIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);
        if (roleIds.Count == 0)
        {
            return [];
        }

        var ids = roleIds.Distinct().ToList();
        var known = await context.Roles
            .Where(role => ids.Contains(role.Id))
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);

        return ids.Except(known).ToList();
    }

    /// <inheritdoc />
    public Task<bool> NameExistsAsync(
        string name,
        RoleId? excluding = null,
        CancellationToken cancellationToken = default)
        => context.Roles.AnyAsync(
            role => role.Name == name && (excluding == null || role.Id != excluding),
            cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsAssignedAsync(RoleId roleId, CancellationToken cancellationToken = default)
        => context.UserRoles.AnyAsync(assignment => assignment.RoleId == roleId, cancellationToken);

    /// <inheritdoc />
    public void Add(Role role) => context.Roles.Add(role);

    /// <inheritdoc />
    public void Remove(Role role) => context.Roles.Remove(role);
}

/// <summary>The Entity Framework Core implementation of <see cref="IPermissionRepository"/>.</summary>
/// <param name="context">The Entity Framework Core session.</param>
internal sealed class PermissionRepository(ApplicationDbContext context) : IPermissionRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Permission>> ListAsync(
        CancellationToken cancellationToken = default)
        => await context.Permissions
            .OrderBy(permission => permission.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Permission>> GetByNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Count == 0)
        {
            return [];
        }

        var wanted = names.ToList();
        return await context.Permissions
            .Where(permission => wanted.Contains(permission.Name))
            .ToListAsync(cancellationToken);
    }
}
