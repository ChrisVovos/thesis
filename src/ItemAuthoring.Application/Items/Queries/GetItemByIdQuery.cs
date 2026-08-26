using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Items.Dtos;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Items.Queries;

/// <summary>Returns the full projection of a single item.</summary>
/// <param name="ItemId">The item to load.</param>
[RequiresPermission(Permissions.ItemsRead)]
public sealed record GetItemByIdQuery(Guid ItemId) : IQuery<Result<ItemDetailDto>>;

/// <summary>Handles <see cref="GetItemByIdQuery"/>.</summary>
/// <param name="readStore">The read side of the item bank.</param>
internal sealed class GetItemByIdQueryHandler(IItemReadStore readStore)
    : IRequestHandler<GetItemByIdQuery, Result<ItemDetailDto>>
{
    /// <inheritdoc />
    public async Task<Result<ItemDetailDto>> HandleAsync(
        GetItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        var item = await readStore.GetDetailAsync(request.ItemId, cancellationToken);
        return item is null
            ? Result.Failure<ItemDetailDto>(Error.NotFound("item.not_found", "The item does not exist."))
            : Result.Success(item);
    }
}

/// <summary>Returns the published versions of an item, newest first.</summary>
/// <param name="ItemId">The item to load versions for.</param>
[RequiresPermission(Permissions.ItemsRead)]
public sealed record GetItemVersionsQuery(Guid ItemId)
    : IQuery<Result<IReadOnlyList<ItemVersionDto>>>;

/// <summary>Handles <see cref="GetItemVersionsQuery"/>.</summary>
/// <param name="readStore">The read side of the item bank.</param>
internal sealed class GetItemVersionsQueryHandler(IItemReadStore readStore)
    : IRequestHandler<GetItemVersionsQuery, Result<IReadOnlyList<ItemVersionDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ItemVersionDto>>> HandleAsync(
        GetItemVersionsQuery request,
        CancellationToken cancellationToken)
        => Result.Success(await readStore.GetVersionsAsync(request.ItemId, cancellationToken));
}
