using ItemAuthoring.Domain.Exams;
using ItemAuthoring.Domain.Items;
using ItemAuthoring.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItemAuthoring.Infrastructure.Persistence.Configurations;

/// <summary>Maps the exam aggregate root.</summary>
internal sealed class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.ToTable("Exams", table => table.HasCheckConstraint(
            "CK_Exams_PassingScore_Range",
            "[PassingScorePercentage] BETWEEN 0 AND 100"));

        builder.HasKey(exam => exam.Id);

        builder.Property(exam => exam.Title)
            .HasConversion<ExamTitleConverter>()
            .HasMaxLength(ExamTitle.MaxLength)
            .IsRequired();

        builder.Property(exam => exam.Description).HasMaxLength(Exam.MaxDescriptionLength);
        builder.Property(exam => exam.Status).IsRequired();
        builder.Property(exam => exam.PassingScorePercentage).IsRequired();
        builder.Property(exam => exam.OwnerId).IsRequired();
        builder.Property(exam => exam.IsDeleted).IsRequired();

        builder.HasMany(exam => exam.Sections)
            .WithOne()
            .HasForeignKey(section => section.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Exam.Sections))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(exam => exam.Status);
        builder.HasIndex(exam => exam.OwnerId);
        builder.HasQueryFilter(exam => !exam.IsDeleted);
    }
}

/// <summary>Maps the sections of an exam.</summary>
internal sealed class ExamSectionConfiguration : IEntityTypeConfiguration<ExamSection>
{
    public void Configure(EntityTypeBuilder<ExamSection> builder)
    {
        builder.ToTable("ExamSections");
        builder.HasKey(section => section.Id);

        builder.Property(section => section.Title)
            .HasMaxLength(ExamSection.MaxTitleLength)
            .IsRequired();
        builder.Property(section => section.Instructions)
            .HasMaxLength(ExamSection.MaxInstructionsLength);
        builder.Property(section => section.Position).IsRequired();

        builder.HasMany(section => section.Items)
            .WithOne()
            .HasForeignKey(item => item.ExamSectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(ExamSection.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(section => new { section.ExamId, section.Position }).IsUnique();
    }
}

/// <summary>Maps the placement of a bank item inside an exam section.</summary>
internal sealed class ExamItemConfiguration : IEntityTypeConfiguration<ExamItem>
{
    public void Configure(EntityTypeBuilder<ExamItem> builder)
    {
        builder.ToTable("ExamItems");
        builder.HasKey(placement => placement.Id);

        builder.Property(placement => placement.Position).IsRequired();
        builder.Property(placement => placement.ScoreOverride)
            .HasConversion<PointsConverter>()
            .HasColumnName("ScoreOverride");

        // Restrict, not cascade: deleting an item that an exam still references must fail loudly
        // rather than silently rewrite the composition of an assembled examination.
        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(placement => placement.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(placement => new { placement.ExamSectionId, placement.Position }).IsUnique();
        builder.HasIndex(placement => placement.ItemId);
    }
}
