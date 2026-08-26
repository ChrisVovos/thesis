using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Exams.Dtos;
using ItemAuthoring.Domain.Exams;
using Microsoft.EntityFrameworkCore;

namespace ItemAuthoring.Infrastructure.Persistence.ReadStores;

/// <summary>The Entity Framework Core implementation of <see cref="IExamReadStore"/>.</summary>
/// <param name="context">The Entity Framework Core session.</param>
/// <param name="items">The read side of the item bank, used to hydrate placements.</param>
internal sealed class ExamReadStore(ApplicationDbContext context, IItemReadStore items) : IExamReadStore
{
    /// <inheritdoc />
    public IQueryable<ExamSummaryDto> QuerySummaries()
        => context.Exams
            .AsNoTracking()
            .Select(exam => new ExamSummaryDto
            {
                Id = exam.Id.Value,
                Title = exam.Title.Value,
                Description = exam.Description,
                Status = exam.Status,
                TimeLimitMinutes = exam.TimeLimitMinutes,
                PassingScorePercentage = exam.PassingScorePercentage,
                OwnerId = exam.OwnerId.Value,
                OwnerName = context.Users
                    .Where(user => user.Id == exam.OwnerId)
                    .Select(user => user.DisplayName.Value)
                    .FirstOrDefault()!,
                SectionCount = exam.Sections.Count,
                ItemCount = exam.Sections.Sum(section => section.Items.Count),
                TotalScore = context.ExamItems
                    .Where(placement => exam.Sections
                        .Select(section => section.Id)
                        .Contains(placement.ExamSectionId))
                    .Sum(placement => placement.ScoreOverride == null
                        ? context.Items
                            .Where(item => item.Id == placement.ItemId)
                            .Select(item => item.MaximumScore.Value)
                            .FirstOrDefault()
                        : placement.ScoreOverride.Value),
                CreatedAtUtc = exam.CreatedAtUtc,
                PublishedAtUtc = exam.PublishedAtUtc,
            });

    /// <inheritdoc />
    public async Task<ExamDetailDto?> GetDetailAsync(
        Guid examId,
        CancellationToken cancellationToken = default)
    {
        var summary = await QuerySummaries()
            .FirstOrDefaultAsync(exam => exam.Id == examId, cancellationToken);

        if (summary is null)
        {
            return null;
        }

        var sections = await LoadSectionsAsync([examId], cancellationToken);
        var exam = await context.Exams
            .Include(entity => entity.Sections)
            .ThenInclude(section => section.Items)
            .AsNoTracking()
            .FirstAsync(entity => entity.Id == new ExamId(examId), cancellationToken);

        return new ExamDetailDto
        {
            Summary = summary,
            Sections = sections.TryGetValue(examId, out var value) ? value : [],
            CompositionViolations = exam.ValidateComposition(),
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ExamSectionDto>>> GetSectionsAsync(
        IReadOnlyList<Guid> examIds,
        CancellationToken cancellationToken = default)
        => await LoadSectionsAsync(examIds, cancellationToken);

    private async Task<Dictionary<Guid, IReadOnlyList<ExamSectionDto>>> LoadSectionsAsync(
        IReadOnlyList<Guid> examIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(examIds);
        if (examIds.Count == 0)
        {
            return [];
        }

        var ids = examIds.Select(id => new ExamId(id)).ToList();
        var rows = await context.ExamSections
            .AsNoTracking()
            .Where(section => ids.Contains(section.ExamId))
            .OrderBy(section => section.Position)
            .Select(section => new
            {
                ExamId = section.ExamId.Value,
                Section = new
                {
                    Id = section.Id.Value,
                    section.Title,
                    section.Instructions,
                    section.Position,
                    Items = section.Items
                        .OrderBy(placement => placement.Position)
                        .Select(placement => new
                        {
                            Id = placement.Id.Value,
                            ItemId = placement.ItemId.Value,
                            placement.Position,
                            ScoreOverride = placement.ScoreOverride == null
                                ? (decimal?)null
                                : placement.ScoreOverride.Value,
                            ItemScore = context.Items
                                .Where(item => item.Id == placement.ItemId)
                                .Select(item => item.MaximumScore.Value)
                                .FirstOrDefault(),
                        })
                        .ToList(),
                },
            })
            .ToListAsync(cancellationToken);

        var referencedItemIds = rows
            .SelectMany(row => row.Section.Items)
            .Select(placement => placement.ItemId)
            .Distinct()
            .ToList();

        var itemSummaries = await items.GetSummariesAsync(referencedItemIds, cancellationToken);

        return rows
            .GroupBy(row => row.ExamId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ExamSectionDto>)group
                    .Select(row => new ExamSectionDto
                    {
                        Id = row.Section.Id,
                        Title = row.Section.Title,
                        Instructions = row.Section.Instructions,
                        Position = row.Section.Position,
                        Items = row.Section.Items
                            .Select(placement => new ExamItemDto
                            {
                                Id = placement.Id,
                                ItemId = placement.ItemId,
                                Position = placement.Position,
                                ScoreOverride = placement.ScoreOverride,
                                EffectiveScore = placement.ScoreOverride ?? placement.ItemScore,
                                Item = itemSummaries.GetValueOrDefault(placement.ItemId),
                            })
                            .ToList(),
                    })
                    .ToList());
    }
}
