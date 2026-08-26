using ItemAuthoring.Domain.Exams;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;
using ItemAuthoring.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace ItemAuthoring.Infrastructure.Persistence;

/// <summary>
/// The Entity Framework Core session for the item authoring database.
/// </summary>
/// <remarks>
/// <para>
/// The context is the unit of work of the application; <see cref="UnitOfWork"/> exists only to keep
/// the application layer free of an Entity Framework Core reference, and adds no behaviour of its own.
/// </para>
/// <para>
/// Every soft-deletable aggregate carries a global query filter, so "deleted" content is invisible to
/// reads by default rather than by convention. Forgetting a <c>Where(x =&gt; !x.IsDeleted)</c> is a
/// disclosure bug; making it a model-level rule removes the possibility.
/// </para>
/// </remarks>
/// <param name="options">The context options supplied by the composition root.</param>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    /// <summary>Gets the item bank.</summary>
    public DbSet<Item> Items => Set<Item>();

    /// <summary>Gets the answer options of choice items.</summary>
    public DbSet<ItemOption> ItemOptions => Set<ItemOption>();

    /// <summary>Gets the immutable published item versions.</summary>
    public DbSet<ItemVersion> ItemVersions => Set<ItemVersion>();

    /// <summary>Gets the item-to-tag associations.</summary>
    public DbSet<ItemTag> ItemTags => Set<ItemTag>();

    /// <summary>Gets the category taxonomy.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Gets the tags.</summary>
    public DbSet<Tag> Tags => Set<Tag>();

    /// <summary>Gets the exams.</summary>
    public DbSet<Exam> Exams => Set<Exam>();

    /// <summary>Gets the exam sections.</summary>
    public DbSet<ExamSection> ExamSections => Set<ExamSection>();

    /// <summary>Gets the exam item placements.</summary>
    public DbSet<ExamItem> ExamItems => Set<ExamItem>();

    /// <summary>Gets the user directory.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets the roles.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Gets the permission catalogue.</summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>Gets the user-to-role assignments.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>Gets the role-to-permission grants.</summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>Gets the issued refresh tokens.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema("authoring");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties<ItemId>().HaveConversion<StronglyTypedIdConverter<ItemId>>();
        configurationBuilder.Properties<ItemOptionId>()
            .HaveConversion<StronglyTypedIdConverter<ItemOptionId>>();
        configurationBuilder.Properties<ItemVersionId>()
            .HaveConversion<StronglyTypedIdConverter<ItemVersionId>>();
        configurationBuilder.Properties<CategoryId>()
            .HaveConversion<StronglyTypedIdConverter<CategoryId>>();
        configurationBuilder.Properties<TagId>().HaveConversion<StronglyTypedIdConverter<TagId>>();
        configurationBuilder.Properties<ExamId>().HaveConversion<StronglyTypedIdConverter<ExamId>>();
        configurationBuilder.Properties<ExamSectionId>()
            .HaveConversion<StronglyTypedIdConverter<ExamSectionId>>();
        configurationBuilder.Properties<ExamItemId>()
            .HaveConversion<StronglyTypedIdConverter<ExamItemId>>();
        configurationBuilder.Properties<UserId>().HaveConversion<StronglyTypedIdConverter<UserId>>();
        configurationBuilder.Properties<RoleId>().HaveConversion<StronglyTypedIdConverter<RoleId>>();
        configurationBuilder.Properties<PermissionId>()
            .HaveConversion<StronglyTypedIdConverter<PermissionId>>();
        configurationBuilder.Properties<RefreshTokenId>()
            .HaveConversion<StronglyTypedIdConverter<RefreshTokenId>>();

        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }
}
