using ItemAuthoring.Domain.Items;
using ItemAuthoring.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItemAuthoring.Infrastructure.Persistence.Configurations;

/// <summary>Maps the answer options of choice items.</summary>
internal sealed class ItemOptionConfiguration : IEntityTypeConfiguration<ItemOption>
{
    public void Configure(EntityTypeBuilder<ItemOption> builder)
    {
        builder.ToTable("ItemOptions");
        builder.HasKey(option => option.Id);

        builder.Property(option => option.Text)
            .HasConversion<OptionTextConverter>()
            .HasMaxLength(OptionText.MaxLength)
            .IsRequired();

        builder.Property(option => option.Feedback).HasMaxLength(ItemOption.MaxFeedbackLength);
        builder.Property(option => option.Position).IsRequired();
        builder.Property(option => option.IsCorrect).IsRequired();

        builder.HasIndex(option => new { option.ItemId, option.Position }).IsUnique();
    }
}

/// <summary>Maps the immutable published item versions and their frozen options.</summary>
internal sealed class ItemVersionConfiguration : IEntityTypeConfiguration<ItemVersion>
{
    public void Configure(EntityTypeBuilder<ItemVersion> builder)
    {
        builder.ToTable("ItemVersions");
        builder.HasKey(version => version.Id);

        builder.Property(version => version.StemText)
            .HasMaxLength(ItemStem.MaxLength)
            .IsRequired();
        builder.Property(version => version.RubricGuidance)
            .HasMaxLength(EssayRubric.MaxGuidanceLength);
        builder.Property(version => version.MaximumScore).IsRequired();
        builder.Property(version => version.VersionNumber).IsRequired();
        builder.Property(version => version.PublishedAtUtc).IsRequired();

        builder.HasIndex(version => new { version.ItemId, version.VersionNumber }).IsUnique();

        builder.OwnsMany(version => version.Options, option =>
        {
            option.ToTable("ItemVersionOptions");
            option.WithOwner().HasForeignKey("ItemVersionId");
            option.Property(value => value.Text).HasMaxLength(OptionText.MaxLength).IsRequired();
            option.Property(value => value.Feedback).HasMaxLength(ItemOption.MaxFeedbackLength);
            // Position is the ordinal the author gave the option, not a surrogate: the owned
            // collection convention would otherwise make it store generated and the second option of
            // a version would be saved as an update of a row that does not exist.
            option.Property(value => value.Position).ValueGeneratedNever().IsRequired();
            option.Property(value => value.IsCorrect).IsRequired();
            option.HasKey("ItemVersionId", "Position");
        });

        builder.Metadata.FindNavigation(nameof(ItemVersion.Options))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Maps the item-to-tag association table.</summary>
internal sealed class ItemTagConfiguration : IEntityTypeConfiguration<ItemTag>
{
    public void Configure(EntityTypeBuilder<ItemTag> builder)
    {
        builder.ToTable("ItemTags");
        builder.HasKey(association => new { association.ItemId, association.TagId });

        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(association => association.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(association => association.TagId);
    }
}
