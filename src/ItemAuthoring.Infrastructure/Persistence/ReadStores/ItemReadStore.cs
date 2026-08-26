using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Items.Dtos;
using ItemAuthoring.Domain.Items;
using Microsoft.EntityFrameworkCore;

namespace ItemAuthoring.Infrastructure.Persistence.ReadStores;

/// <summary>
/// The Entity Framework Core implementation of <see cref="IItemReadStore"/>.
/// </summary>
/// <remarks>
/// Every projection is expressed entirely in the expression tree, so filters appended later — by the
/// REST query handler or by the Hot Chocolate filtering middleware — still reach SQL Server. Nothing
/// here materializes an aggregate; the read side never loads behaviour it does not use.
/// </remarks>
/// <param name="context">The Entity Framework Core session.</param>
internal sealed class ItemReadStore(ApplicationDbContext context) : IItemReadStore
{
    /// <inheritdoc />
    public IQueryable<ItemSummaryDto> QuerySummaries()
        => context.Items
            .AsNoTracking()
            .Select(item => new ItemSummaryDto
            {
                Id = item.Id.Value,
                Type = item.Type,
                Status = item.Status,
                Difficulty = item.Difficulty,
                Stem = item.Stem.Text,
                MaximumScore = item.MaximumScore.Value,
                CategoryId = item.CategoryId.Value,
                CategoryName = context.Categories
                    .Where(category => category.Id == item.CategoryId)
                    .Select(category => category.Name.Value)
                    .FirstOrDefault()!,
                AuthorId = item.AuthorId.Value,
                AuthorName = context.Users
                    .Where(user => user.Id == item.AuthorId)
                    .Select(user => user.DisplayName.Value)
                    .FirstOrDefault()!,
                VersionNumber = item.VersionNumber,
                CreatedAtUtc = item.CreatedAtUtc,
                LastModifiedAtUtc = item.LastModifiedAtUtc,
                Tags = context.ItemTags
                    .Where(association => association.ItemId == item.Id)
                    .Join(
                        context.Tags,
                        association => association.TagId,
                        tag => tag.Id,
                        (_, tag) => new ItemTagDto { Id = tag.Id.Value, Name = tag.Name.Value })
                    .ToList(),
            });

    /// <inheritdoc />
    public async Task<ItemDetailDto?> GetDetailAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var summary = await QuerySummaries()
            .FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);

        if (summary is null)
        {
            return null;
        }

        var id = new ItemId(itemId);
        var options = await QueryOptions(option => option.ItemId == id).ToListAsync(cancellationToken);
        var essay = await context.Items
            .AsNoTracking()
            .OfType<EssayItem>()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Rubric.Guidance,
                item.Rubric.MinimumWords,
                item.Rubric.MaximumWords,
                item.SampleAnswer,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new ItemDetailDto
        {
            Summary = summary,
            Options = options,
            RubricGuidance = essay?.Guidance,
            RubricMinimumWords = essay?.MinimumWords,
            RubricMaximumWords = essay?.MaximumWords,
            SampleAnswer = essay?.SampleAnswer,
            Versions = await GetVersionsAsync(itemId, cancellationToken),
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ItemSummaryDto>> GetSummariesAsync(
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemIds);
        if (itemIds.Count == 0)
        {
            return new Dictionary<Guid, ItemSummaryDto>();
        }

        var summaries = await QuerySummaries()
            .Where(item => itemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        return summaries.ToDictionary(item => item.Id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemOptionDto>>> GetOptionsAsync(
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemIds);
        if (itemIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<ItemOptionDto>>();
        }

        var ids = itemIds.Select(id => new ItemId(id)).ToList();
        var rows = await context.ItemOptions
            .AsNoTracking()
            .Where(option => ids.Contains(option.ItemId))
            .OrderBy(option => option.Position)
            .Select(option => new
            {
                ItemId = option.ItemId.Value,
                Option = new ItemOptionDto
                {
                    Id = option.Id.Value,
                    Text = option.Text.Text,
                    IsCorrect = option.IsCorrect,
                    Position = option.Position,
                    Feedback = option.Feedback,
                },
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.ItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ItemOptionDto>)group.Select(row => row.Option).ToList());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemVersionDto>> GetVersionsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var id = new ItemId(itemId);
        return await context.ItemVersions
            .AsNoTracking()
            .Where(version => version.ItemId == id)
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => new ItemVersionDto
            {
                Id = version.Id.Value,
                VersionNumber = version.VersionNumber,
                PublishedAtUtc = version.PublishedAtUtc,
                StemText = version.StemText,
                Difficulty = version.Difficulty,
                MaximumScore = version.MaximumScore,
                Options = version.Options
                    .OrderBy(option => option.Position)
                    .Select(option => new ItemOptionDto
                    {
                        Text = option.Text,
                        IsCorrect = option.IsCorrect,
                        Position = option.Position,
                        Feedback = option.Feedback,
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<ItemOptionDto> QueryOptions(
        System.Linq.Expressions.Expression<Func<ItemOption, bool>> predicate)
        => context.ItemOptions
            .AsNoTracking()
            .Where(predicate)
            .OrderBy(option => option.Position)
            .Select(option => new ItemOptionDto
            {
                Id = option.Id.Value,
                Text = option.Text.Text,
                IsCorrect = option.IsCorrect,
                Position = option.Position,
                Feedback = option.Feedback,
            });
}
