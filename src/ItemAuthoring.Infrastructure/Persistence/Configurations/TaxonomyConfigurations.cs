using ItemAuthoring.Domain.Items;
using ItemAuthoring.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItemAuthoring.Infrastructure.Persistence.Configurations;

/// <summary>Maps the category taxonomy.</summary>
internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasConversion<CategoryNameConverter>()
            .HasMaxLength(CategoryName.MaxLength)
            .IsRequired();

        builder.Property(category => category.Description)
            .HasMaxLength(Category.MaxDescriptionLength);
        builder.Property(category => category.IsActive).IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sibling uniqueness only: two different subjects may both contain a topic called "Algebra".
        builder.HasIndex(category => new { category.ParentCategoryId, category.Name }).IsUnique();
    }
}

/// <summary>Maps the tags.</summary>
/// <remarks>
/// The normalized label is persisted as its own column and carries the unique index, so tag identity
/// does not depend on the collation configured for the database.
/// </remarks>
internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(tag => tag.Id);

        builder.OwnsOne(tag => tag.Name, name =>
        {
            name.Property(value => value.Value)
                .HasColumnName("Name")
                .HasMaxLength(TagName.MaxLength)
                .IsRequired();
            name.Property(value => value.Normalized)
                .HasColumnName("NormalizedName")
                .HasMaxLength(TagName.MaxLength)
                .IsRequired();
            name.HasIndex(value => value.Normalized).IsUnique();
        });

        builder.Navigation(tag => tag.Name).IsRequired();
    }
}
