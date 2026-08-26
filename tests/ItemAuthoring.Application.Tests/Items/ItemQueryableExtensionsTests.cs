using ItemAuthoring.Application.Items.Dtos;
using ItemAuthoring.Application.Items.Queries;
using ItemAuthoring.Domain.Items;
using Shouldly;

namespace ItemAuthoring.Application.Tests.Items;

/// <summary>
/// Exercises the shared filter definition that both API surfaces compose on to.
/// </summary>
/// <remarks>
/// The expressions are evaluated in memory here. Their translation to SQL is covered by the
/// integration suite; what matters at this level is that the predicates select the right rows, because
/// a mistake here would silently change the results of both REST and GraphQL at once.
/// </remarks>
public sealed class ItemQueryableExtensionsTests
{
    private static readonly Guid Algebra = Guid.CreateVersion7();
    private static readonly Guid Geometry = Guid.CreateVersion7();
    private static readonly Guid Mathematics = Guid.CreateVersion7();
    private static readonly Guid Physics = Guid.CreateVersion7();

    private static IQueryable<ItemSummaryDto> Bank() => new[]
    {
        Item("Add two integers", ItemType.MultipleChoiceSingleResponse, ItemStatus.Published,
            DifficultyLevel.Easy, Mathematics, "Mathematics", [Algebra]),
        Item("Prove the triangle inequality", ItemType.Essay, ItemStatus.Draft,
            DifficultyLevel.Hard, Mathematics, "Mathematics", [Algebra, Geometry]),
        Item("Newton's second law", ItemType.EitherOr, ItemStatus.Published,
            DifficultyLevel.Medium, Physics, "Physics", []),
    }.AsQueryable();

    private static ItemSummaryDto Item(
        string stem,
        ItemType type,
        ItemStatus status,
        DifficultyLevel difficulty,
        Guid categoryId,
        string categoryName,
        Guid[] tagIds) => new()
        {
            Id = Guid.CreateVersion7(),
            Stem = stem,
            Type = type,
            Status = status,
            Difficulty = difficulty,
            CategoryId = categoryId,
            CategoryName = categoryName,
            MaximumScore = 1m,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Tags = tagIds.Select(id => new ItemTagDto { Id = id, Name = id.ToString() }).ToList(),
        };

    [Fact]
    public void No_criteria_returns_everything()
        => Bank().ApplyFilters(new ItemSearchCriteria()).Count().ShouldBe(3);

    [Fact]
    public void Filtering_by_status_narrows_the_result()
        => Bank()
            .ApplyFilters(new ItemSearchCriteria { Statuses = [ItemStatus.Published] })
            .Count()
            .ShouldBe(2);

    [Fact]
    public void Filtering_by_type_narrows_the_result()
        => Bank()
            .ApplyFilters(new ItemSearchCriteria { Types = [ItemType.Essay] })
            .Single()
            .Stem
            .ShouldBe("Prove the triangle inequality");

    [Fact]
    public void Filtering_by_category_narrows_the_result()
        => Bank()
            .ApplyFilters(new ItemSearchCriteria { CategoryId = Physics })
            .Count()
            .ShouldBe(1);

    [Fact]
    public void Filtering_by_several_tags_requires_all_of_them()
    {
        Bank().ApplyFilters(new ItemSearchCriteria { TagIds = [Algebra] }).Count().ShouldBe(2);
        Bank().ApplyFilters(new ItemSearchCriteria { TagIds = [Algebra, Geometry] })
            .Count()
            .ShouldBe(1);
    }

    [Fact]
    public void Searching_matches_the_stem_or_the_category_name()
    {
        Bank().ApplyFilters(new ItemSearchCriteria { Search = "triangle" }).Count().ShouldBe(1);
        Bank().ApplyFilters(new ItemSearchCriteria { Search = "Physics" }).Count().ShouldBe(1);
    }

    [Fact]
    public void Filters_combine_conjunctively()
        => Bank()
            .ApplyFilters(new ItemSearchCriteria
            {
                Statuses = [ItemStatus.Published],
                Difficulties = [DifficultyLevel.Medium],
            })
            .Single()
            .Stem
            .ShouldBe("Newton's second law");

    [Fact]
    public void Sorting_by_an_unknown_property_falls_back_to_newest_first()
    {
        var sorted = Bank().ApplySorting(new ItemSearchCriteria { SortBy = "nonsense" }).ToList();

        sorted.Count.ShouldBe(3);
        sorted.ShouldBeInOrder(SortDirection.Descending, Comparer<ItemSummaryDto>.Create(
            (left, right) => left.CreatedAtUtc.CompareTo(right.CreatedAtUtc)));
    }

    [Fact]
    public void Sorting_by_stem_is_honoured_in_both_directions()
    {
        Bank().ApplySorting(new ItemSearchCriteria { SortBy = "stem" }).First().Stem
            .ShouldBe("Add two integers");
        Bank().ApplySorting(new ItemSearchCriteria { SortBy = "stem", SortDescending = true })
            .First()
            .Stem
            .ShouldBe("Prove the triangle inequality");
    }

    [Fact]
    public void The_page_size_is_clamped_rather_than_trusted()
    {
        new ItemSearchCriteria { PageSize = 10_000 }.PageSize
            .ShouldBe(Common.PagedQuery.MaxPageSize);
        new ItemSearchCriteria { PageSize = 0 }.PageSize
            .ShouldBe(Common.PagedQuery.DefaultPageSize);
        new ItemSearchCriteria { Page = -5 }.Page.ShouldBe(1);
    }

    [Fact]
    public void The_skip_count_follows_from_the_page_and_size()
        => new ItemSearchCriteria { Page = 3, PageSize = 25 }.Skip.ShouldBe(50);
}
