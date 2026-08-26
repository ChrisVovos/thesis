using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Items.Dtos;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Items.Queries;

/// <summary>Returns one page of the item bank.</summary>
/// <param name="Criteria">The search, filter, sort and paging criteria.</param>
[RequiresPermission(Permissions.ItemsRead)]
public sealed record SearchItemsQuery(ItemSearchCriteria Criteria)
    : IQuery<Result<PagedResult<ItemSummaryDto>>>;

/// <summary>Handles <see cref="SearchItemsQuery"/>.</summary>
/// <param name="readStore">The read side of the item bank.</param>
/// <param name="executor">The asynchronous query executor.</param>
internal sealed class SearchItemsQueryHandler(IItemReadStore readStore, IAsyncQueryExecutor executor)
    : IRequestHandler<SearchItemsQuery, Result<PagedResult<ItemSummaryDto>>>
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<ItemSummaryDto>>> HandleAsync(
        SearchItemsQuery request,
        CancellationToken cancellationToken)
    {
        var criteria = request.Criteria;
        var filtered = readStore.QuerySummaries().ApplyFilters(criteria);

        var totalCount = await executor.CountAsync(filtered, cancellationToken);
        if (totalCount == 0)
        {
            return Result.Success(PagedResult<ItemSummaryDto>.Empty(criteria.Page, criteria.PageSize));
        }

        var page = await executor.ToListAsync(
            filtered.ApplySorting(criteria).Skip(criteria.Skip).Take(criteria.PageSize),
            cancellationToken);

        return Result.Success(
            new PagedResult<ItemSummaryDto>(page, totalCount, criteria.Page, criteria.PageSize));
    }
}
