using ItemAuthoring.Domain.Items;
using ItemAuthoring.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItemAuthoring.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the polymorphic item hierarchy onto a single table.
/// </summary>
/// <remarks>
/// Table-per-hierarchy is chosen over table-per-type because every read in this application is
/// polymorphic — the item bank grid, the exam builder picker and the GraphQL <c>items</c> field all
/// query across all four shapes. Under table-per-type each of those reads becomes a four-way join or
/// a union, which would add a persistence artefact to precisely the queries the study measures. The
/// price is a handful of nullable columns for the essay-specific fields, which filtered indexes and
/// the check constraint below keep honest.
/// </remarks>
internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items", table => table.HasCheckConstraint(
            "CK_Items_MaximumScore_Positive",
            "[MaximumScore] > 0"));

        builder.HasKey(item => item.Id);

        builder.HasDiscriminator(item => item.Type)
            .HasValue<SingleResponseItem>(ItemType.MultipleChoiceSingleResponse)
            .HasValue<MultipleResponseItem>(ItemType.MultipleChoiceMultipleResponse)
            .HasValue<EssayItem>(ItemType.Essay)
            .HasValue<EitherOrItem>(ItemType.EitherOr);

        builder.Property(item => item.Stem)
            .HasConversion<ItemStemConverter>()
            .HasMaxLength(ItemStem.MaxLength)
            .IsRequired();

        builder.Property(item => item.MaximumScore)
            .HasConversion<PointsConverter>()
            .HasColumnName("MaximumScore")
            .IsRequired();

        builder.Property(item => item.Status).IsRequired();
        builder.Property(item => item.Difficulty).IsRequired();
        builder.Property(item => item.CategoryId).IsRequired();
        builder.Property(item => item.AuthorId).IsRequired();
        builder.Property(item => item.VersionNumber).IsRequired();
        builder.Property(item => item.IsDeleted).IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(item => item.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(item => item.Tags).AutoInclude(false);
        builder.Navigation(item => item.Versions).AutoInclude(false);

        builder.HasMany(item => item.Tags)
            .WithOne()
            .HasForeignKey(tag => tag.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(item => item.Versions)
            .WithOne()
            .HasForeignKey(version => version.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => item.Status);
        builder.HasIndex(item => item.CategoryId);
        builder.HasIndex(item => new { item.Type, item.Difficulty });
        builder.HasIndex(item => item.IsDeleted).HasFilter("[IsDeleted] = 0");

        builder.HasQueryFilter(item => !item.IsDeleted);

        builder.Metadata.FindNavigation(nameof(Item.Tags))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Item.Versions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Maps the option collection owned by choice items.</summary>
internal sealed class ChoiceItemConfiguration : IEntityTypeConfiguration<ChoiceItem>
{
    public void Configure(EntityTypeBuilder<ChoiceItem> builder)
    {
        builder.HasMany(item => item.Options)
            .WithOne()
            .HasForeignKey(option => option.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(ChoiceItem.Options))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Maps the essay-specific columns of the item table.</summary>
internal sealed class EssayItemConfiguration : IEntityTypeConfiguration<EssayItem>
{
    public void Configure(EntityTypeBuilder<EssayItem> builder)
    {
        builder.Property(item => item.SampleAnswer).HasMaxLength(EssayItem.MaxSampleAnswerLength);

        builder.OwnsOne(item => item.Rubric, rubric =>
        {
            rubric.Property(value => value.Guidance)
                .HasColumnName("RubricGuidance")
                .HasMaxLength(EssayRubric.MaxGuidanceLength);
            rubric.Property(value => value.MinimumWords).HasColumnName("RubricMinimumWords");
            rubric.Property(value => value.MaximumWords).HasColumnName("RubricMaximumWords");
        });

        builder.Navigation(item => item.Rubric).IsRequired();
    }
}
