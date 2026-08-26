using ItemAuthoring.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItemAuthoring.Infrastructure.Persistence.Configurations;

/// <summary>Maps the role aggregate root.</summary>
internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(role => role.Id);

        builder.Property(role => role.Name).HasMaxLength(Role.MaxNameLength).IsRequired();
        builder.Property(role => role.Description)
            .HasMaxLength(Role.MaxDescriptionLength)
            .IsRequired();
        builder.Property(role => role.IsSystemRole).IsRequired();

        builder.HasIndex(role => role.Name).IsUnique();

        builder.HasMany(role => role.Permissions)
            .WithOne()
            .HasForeignKey(grant => grant.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Role.Permissions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Maps the permission catalogue.</summary>
internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Name)
            .HasMaxLength(Permission.MaxNameLength)
            .IsRequired();
        builder.Property(permission => permission.Description)
            .HasMaxLength(Permission.MaxDescriptionLength)
            .IsRequired();

        builder.HasIndex(permission => permission.Name).IsUnique();
    }
}

/// <summary>Maps the role-to-permission grant table.</summary>
internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(grant => new { grant.RoleId, grant.PermissionId });

        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(grant => grant.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(grant => grant.PermissionId);
    }
}
