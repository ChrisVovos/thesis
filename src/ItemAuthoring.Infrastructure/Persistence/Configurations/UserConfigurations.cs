using ItemAuthoring.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItemAuthoring.Infrastructure.Persistence.Configurations;

/// <summary>Maps the user aggregate root.</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);

        builder.OwnsOne(user => user.Email, email =>
        {
            email.Property(value => value.Value)
                .HasColumnName("Email")
                .HasMaxLength(EmailAddress.MaxLength)
                .IsRequired();
            email.Property(value => value.Normalized)
                .HasColumnName("NormalizedEmail")
                .HasMaxLength(EmailAddress.MaxLength)
                .IsRequired();
            email.HasIndex(value => value.Normalized).IsUnique();
        });

        builder.OwnsOne(user => user.DisplayName, name => name
            .Property(value => value.Value)
            .HasColumnName("DisplayName")
            .HasMaxLength(DisplayName.MaxLength)
            .IsRequired());

        builder.OwnsOne(user => user.PasswordHash, hash => hash
            .Property(value => value.Value)
            .HasColumnName("PasswordHash")
            .HasMaxLength(PasswordHash.MaxLength)
            .IsRequired());

        builder.Navigation(user => user.Email).IsRequired();
        builder.Navigation(user => user.DisplayName).IsRequired();
        builder.Navigation(user => user.PasswordHash).IsRequired();

        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.FailedSignInAttempts).IsRequired();

        builder.HasMany(user => user.Roles)
            .WithOne()
            .HasForeignKey(assignment => assignment.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.RefreshTokens)
            .WithOne()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(User.Roles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(User.RefreshTokens))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Maps the user-to-role assignment table.</summary>
internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(assignment => new { assignment.UserId, assignment.RoleId });

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(assignment => assignment.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(assignment => assignment.RoleId);
    }
}

/// <summary>Maps the refresh tokens issued to a user.</summary>
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(RefreshToken.HashLength)
            .IsRequired();
        builder.Property(token => token.ReplacedByTokenHash).HasMaxLength(RefreshToken.HashLength);
        builder.Property(token => token.IssuedAtUtc).IsRequired();
        builder.Property(token => token.ExpiresAtUtc).IsRequired();

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.ExpiresAtUtc);
    }
}
