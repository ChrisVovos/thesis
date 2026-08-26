using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Items.Dtos;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Items.Queries;

/// <summary>Returns the complete category taxonomy.</summary>
[RequiresPermission(Permissions.ItemsRead)]
public sealed record ListCategoriesQuery : IQuery<Result<IReadOnlyList<CategoryDto>>>;

/// <summary>Returns every tag, ordered by label.</summary>
[RequiresPermission(Permissions.ItemsRead)]
public sealed record ListTagsQuery : IQuery<Result<IReadOnlyList<TagDto>>>;

/// <summary>Handles <see cref="ListCategoriesQuery"/>.</summary>
/// <param name="readStore">The read side of the taxonomy.</param>
/// <param name="executor">The asynchronous query executor.</param>
internal sealed class ListCategoriesQueryHandler(
    ITaxonomyReadStore readStore,
    IAsyncQueryExecutor executor)
    : IRequestHandler<ListCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CategoryDto>>> HandleAsync(
        ListCategoriesQuery request,
        CancellationToken cancellationToken)
        => Result.Success(await executor.ToListAsync(
            readStore.QueryCategories().OrderBy(category => category.Name),
            cancellationToken));
}

/// <summary>Handles <see cref="ListTagsQuery"/>.</summary>
/// <param name="readStore">The read side of the taxonomy.</param>
/// <param name="executor">The asynchronous query executor.</param>
internal sealed class ListTagsQueryHandler(ITaxonomyReadStore readStore, IAsyncQueryExecutor executor)
    : IRequestHandler<ListTagsQuery, Result<IReadOnlyList<TagDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TagDto>>> HandleAsync(
        ListTagsQuery request,
        CancellationToken cancellationToken)
        => Result.Success(await executor.ToListAsync(
            readStore.QueryTags().OrderBy(tag => tag.Name),
            cancellationToken));
}
