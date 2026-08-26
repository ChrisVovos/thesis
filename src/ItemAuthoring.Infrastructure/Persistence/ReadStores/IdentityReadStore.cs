using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Identity.Dtos;
using ItemAuthoring.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace ItemAuthoring.Infrastructure.Persistence.ReadStores;

/// <summary>The Entity Framework Core implementation of <see cref="IIdentityReadStore"/>.</summary>
/// <param name="context">The Entity Framework Core session.</param>
internal sealed class IdentityReadStore(ApplicationDbContext context) : IIdentityReadStore
{
    /// <inheritdoc />
    public IQueryable<UserDto> QueryUsers()
        => context.Users
            .AsNoTracking()
            .Select(user => new UserDto
            {
                Id = user.Id.Value,
                Email = user.Email.Value,
                DisplayName = user.DisplayName.Value,
                IsActive = user.IsActive,
                LastSignInAtUtc = user.LastSignInAtUtc,
                CreatedAtUtc = user.CreatedAtUtc,
                Roles = context.UserRoles
                    .Where(assignment => assignment.UserId == user.Id)
                    .Join(
                        context.Roles,
                        assignment => assignment.RoleId,
                        role => role.Id,
                        (_, role) => new RoleDto
                        {
                            Id = role.Id.Value,
                            Name = role.Name,
                            Description = role.Description,
                            IsSystemRole = role.IsSystemRole,
                        })
                    .ToList(),
            });

    /// <inheritdoc />
    public IQueryable<RoleDto> QueryRoles()
        => context.Roles
            .AsNoTracking()
            .Select(role => new RoleDto
            {
                Id = role.Id.Value,
                Name = role.Name,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole,
                UserCount = context.UserRoles.Count(assignment => assignment.RoleId == role.Id),
                Permissions = context.RolePermissions
                    .Where(grant => grant.RoleId == role.Id)
                    .Join(
                        context.Permissions,
                        grant => grant.PermissionId,
                        permission => permission.Id,
                        (_, permission) => new PermissionDto
                        {
                            Id = permission.Id.Value,
                            Name = permission.Name,
                            Description = permission.Description,
                        })
                    .ToList(),
            });

    /// <inheritdoc />
    public Task<UserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => QueryUsers().FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(
        CancellationToken cancellationToken = default)
        => await context.Permissions
            .AsNoTracking()
            .OrderBy(permission => permission.Name)
            .Select(permission => new PermissionDto
            {
                Id = permission.Id.Value,
                Name = permission.Name,
                Description = permission.Description,
            })
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<RoleDto>>> GetRolesByUserAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<RoleDto>>();
        }

        var ids = userIds.Select(id => new UserId(id)).ToList();
        var rows = await context.UserRoles
            .AsNoTracking()
            .Where(assignment => ids.Contains(assignment.UserId))
            .Join(
                context.Roles,
                assignment => assignment.RoleId,
                role => role.Id,
                (assignment, role) => new
                {
                    UserId = assignment.UserId.Value,
                    Role = new RoleDto
                    {
                        Id = role.Id.Value,
                        Name = role.Name,
                        Description = role.Description,
                        IsSystemRole = role.IsSystemRole,
                    },
                })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<RoleDto>)group.Select(row => row.Role).ToList());
    }
}
