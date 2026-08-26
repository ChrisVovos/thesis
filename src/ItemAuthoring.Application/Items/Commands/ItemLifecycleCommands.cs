using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Abstractions.Time;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Items.Commands;

/// <summary>Submits a draft item for reviewer attention.</summary>
/// <param name="ItemId">The item to submit.</param>
[RequiresPermission(Permissions.ItemsSubmit)]
public sealed record SubmitItemForReviewCommand(Guid ItemId) : ICommand<Result>;

/// <summary>Records a reviewer's acceptance of an item.</summary>
/// <param name="ItemId">The item to approve.</param>
[RequiresPermission(Permissions.ItemsReview)]
public sealed record ApproveItemCommand(Guid ItemId) : ICommand<Result>;

/// <summary>Returns an item to its author for further work.</summary>
/// <param name="ItemId">The item to return.</param>
[RequiresPermission(Permissions.ItemsReview)]
public sealed record ReturnItemToDraftCommand(Guid ItemId) : ICommand<Result>;

/// <summary>Freezes an approved item as a new immutable version.</summary>
/// <param name="ItemId">The item to publish.</param>
[RequiresPermission(Permissions.ItemsPublish)]
public sealed record PublishItemCommand(Guid ItemId) : ICommand<Result>;

/// <summary>Withdraws a published item from further use.</summary>
/// <param name="ItemId">The item to retire.</param>
[RequiresPermission(Permissions.ItemsPublish)]
public sealed record RetireItemCommand(Guid ItemId) : ICommand<Result>;

/// <summary>
/// Shared execution shape for the item lifecycle transitions.
/// </summary>
/// <remarks>
/// Every transition performs the same three steps — load, apply, save — and differs only in the
/// aggregate method it calls. Expressing that once removes five near-identical handlers without
/// hiding which transition a given command triggers.
/// </remarks>
/// <param name="items">The item repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal abstract class ItemTransitionHandler(IItemRepository items, IUnitOfWork unitOfWork)
{
    /// <summary>Loads the item, applies the transition and saves.</summary>
    /// <param name="itemId">The item to transition.</param>
    /// <param name="transition">The aggregate method to invoke.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The outcome of the transition.</returns>
    protected async Task<Result> ExecuteAsync(
        Guid itemId,
        Action<Item> transition,
        CancellationToken cancellationToken)
    {
        var item = await items.GetAsync(new ItemId(itemId), cancellationToken);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("item.not_found", "The item does not exist."));
        }

        transition(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="SubmitItemForReviewCommand"/>.</summary>
/// <param name="items">The item repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class SubmitItemForReviewCommandHandler(IItemRepository items, IUnitOfWork unitOfWork)
    : ItemTransitionHandler(items, unitOfWork), IRequestHandler<SubmitItemForReviewCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> HandleAsync(SubmitItemForReviewCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(request.ItemId, item => item.SubmitForReview(), cancellationToken);
}

/// <summary>Handles <see cref="ApproveItemCommand"/>.</summary>
/// <param name="items">The item repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class ApproveItemCommandHandler(IItemRepository items, IUnitOfWork unitOfWork)
    : ItemTransitionHandler(items, unitOfWork), IRequestHandler<ApproveItemCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> HandleAsync(ApproveItemCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(request.ItemId, item => item.Approve(), cancellationToken);
}

/// <summary>Handles <see cref="ReturnItemToDraftCommand"/>.</summary>
/// <param name="items">The item repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class ReturnItemToDraftCommandHandler(IItemRepository items, IUnitOfWork unitOfWork)
    : ItemTransitionHandler(items, unitOfWork), IRequestHandler<ReturnItemToDraftCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> HandleAsync(ReturnItemToDraftCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(request.ItemId, item => item.ReturnToDraft(), cancellationToken);
}

/// <summary>Handles <see cref="PublishItemCommand"/>.</summary>
/// <param name="items">The item repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="clock">The clock supplying the publication instant.</param>
internal sealed class PublishItemCommandHandler(
    IItemRepository items,
    IUnitOfWork unitOfWork,
    IClock clock)
    : ItemTransitionHandler(items, unitOfWork), IRequestHandler<PublishItemCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> HandleAsync(PublishItemCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(request.ItemId, item => item.Publish(clock.UtcNow), cancellationToken);
}

/// <summary>Handles <see cref="RetireItemCommand"/>.</summary>
/// <param name="items">The item repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class RetireItemCommandHandler(IItemRepository items, IUnitOfWork unitOfWork)
    : ItemTransitionHandler(items, unitOfWork), IRequestHandler<RetireItemCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> HandleAsync(RetireItemCommand request, CancellationToken cancellationToken)
        => ExecuteAsync(request.ItemId, item => item.Retire(), cancellationToken);
}
