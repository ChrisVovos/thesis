using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace ItemAuthoring.Infrastructure.Persistence.Repositories;

/// <summary>The Entity Framework Core implementation of <see cref="IUserRepository"/>.</summary>
/// <param name="context">The Entity Framework Core session.</param>
internal sealed class UserRepository(ApplicationDbContext context) : IUserRepository
{
    /// <inheritdoc />
    public Task<User?> GetAsync(UserId userId, CancellationToken cancellationToken = default)
        => context.Users
            .Include(user => user.Roles)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
        => context.Users
            .Include(user => user.Roles)
            .Include(user => user.RefreshTokens)
            .FirstOrDefaultAsync(user => user.Email.Normalized == normalizedEmail, cancellationToken);

    /// <inheritdoc />
    public Task<User?> GetByRefreshTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
        => context.Users
            .Include(user => user.Roles)
            .Include(user => user.RefreshTokens)
            .FirstOrDefaultAsync(
                user => user.RefreshTokens.Any(token => token.TokenHash == tokenHash),
                cancellationToken);

    /// <inheritdoc />
    public Task<bool> EmailExistsAsync(
        string normalizedEmail,
        UserId? excluding = null,
        CancellationToken cancellationToken = default)
        => context.Users.AnyAsync(
            user => user.Email.Normalized == normalizedEmail
                && (excluding == null || user.Id != excluding),
            cancellationToken);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)>
        GetAuthorizationDataAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var roleIds = await context.UserRoles
            .Where(assignment => assignment.UserId == userId)
            .Select(assignment => assignment.RoleId)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
        {
            return ([], []);
        }

        var roleNames = await context.Roles
            .Where(role => roleIds.Contains(role.Id))
            .Select(role => role.Name)
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        var permissionNames = await context.RolePermissions
            .Where(grant => roleIds.Contains(grant.RoleId))
            .Join(
                context.Permissions,
                grant => grant.PermissionId,
                permission => permission.Id,
                (_, permission) => permission.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        return (roleNames, permissionNames);
    }

    /// <inheritdoc />
    public void Add(User user) => context.Users.Add(user);
}
