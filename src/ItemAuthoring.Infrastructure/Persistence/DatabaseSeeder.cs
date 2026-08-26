using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ItemAuthoring.Infrastructure.Persistence;

/// <summary>
/// Brings the database to the state the application needs in order to be usable at all.
/// </summary>
/// <remarks>
/// Seeding is idempotent: it inserts what is missing and leaves what is present alone, so it can run
/// on every start-up including against a database that has already been used.
/// </remarks>
/// <param name="context">The Entity Framework Core session.</param>
/// <param name="passwordHasher">The password hasher.</param>
/// <param name="options">The seeding options.</param>
/// <param name="logger">The logger.</param>
public sealed partial class DatabaseSeeder(
    ApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IOptions<SeedOptions> options,
    ILogger<DatabaseSeeder> logger)
{
    private readonly SeedOptions _options = options.Value;

    /// <summary>Applies migrations if configured, then seeds reference data.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (_options.ApplyMigrations)
        {
            logger.LogInformation("Applying pending database migrations.");
            await context.Database.MigrateAsync(cancellationToken);
        }

        if (!_options.Enabled)
        {
            return;
        }

        await SeedPermissionsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedAdministratorAsync(cancellationToken);

        if (_options.IncludeSampleContent)
        {
            await SeedSampleContentAsync(cancellationToken);
        }
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existing = await context.Permissions
            .Select(permission => permission.Name)
            .ToListAsync(cancellationToken);

        var missing = Permissions.All.Except(existing, StringComparer.Ordinal).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        foreach (var name in missing)
        {
            context.Permissions.Add(Permission.Create(name, DescribePermission(name)));
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} permissions.", missing.Count);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var permissionIds = await context.Permissions
            .ToDictionaryAsync(permission => permission.Name, permission => permission.Id, cancellationToken);

        foreach (var (roleName, permissionNames) in Permissions.DefaultsByRole)
        {
            var role = await context.Roles
                .Include(entity => entity.Permissions)
                .FirstOrDefaultAsync(entity => entity.Name == roleName, cancellationToken);

            if (role is null)
            {
                role = Role.Create(roleName, DescribeRole(roleName), isSystemRole: true);
                context.Roles.Add(role);
            }

            role.ReplacePermissions(permissionNames
                .Where(permissionIds.ContainsKey)
                .Select(name => permissionIds[name]));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdministratorAsync(CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(_options.AdministratorEmail);
        if (await context.Users.AnyAsync(
                user => user.Email.Normalized == email.Normalized, cancellationToken))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.AdministratorPassword))
        {
            throw new InvalidOperationException(
                "Seeding is enabled but no administrator password was supplied. Set "
                + "'Seed:AdministratorPassword' through user secrets or the environment secret store.");
        }

        var administratorRole = await context.Roles
            .FirstAsync(role => role.Name == RoleNames.Administrator, cancellationToken);

        var user = User.Create(
            email,
            DisplayName.Create(_options.AdministratorDisplayName),
            passwordHasher.Hash(_options.AdministratorPassword));
        user.AssignRole(administratorRole.Id);

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded the bootstrap administrator account {Email}.", email.Value);
    }

    private static string DescribeRole(string roleName) => roleName switch
    {
        RoleNames.Administrator => "Full control, including user and role administration.",
        RoleNames.Instructor => "Assembles and publishes examinations from approved items.",
        RoleNames.Author => "Creates and maintains items.",
        RoleNames.Reviewer => "Approves or returns items submitted for review.",
        _ => "A role defined by an administrator.",
    };

    private static string DescribePermission(string name) => name switch
    {
        Permissions.ItemsRead => "Read items and item versions.",
        Permissions.ItemsCreate => "Create new draft items.",
        Permissions.ItemsUpdate => "Edit draft items.",
        Permissions.ItemsDelete => "Logically delete items.",
        Permissions.ItemsSubmit => "Submit a draft item for review.",
        Permissions.ItemsReview => "Approve or return an item that is under review.",
        Permissions.ItemsPublish => "Publish an approved item, or retire a published one.",
        Permissions.ExamsRead => "Read exams.",
        Permissions.ExamsCreate => "Create exams.",
        Permissions.ExamsUpdate => "Change the composition of a draft exam.",
        Permissions.ExamsDelete => "Delete exams.",
        Permissions.ExamsPublish => "Publish or archive an exam.",
        Permissions.TaxonomyManage => "Manage the category and tag taxonomy.",
        Permissions.UsersRead => "Read the user directory.",
        Permissions.UsersManage => "Create, update, activate and deactivate users.",
        Permissions.RolesManage => "Create roles and change their permission sets.",
        _ => name,
    };
}
