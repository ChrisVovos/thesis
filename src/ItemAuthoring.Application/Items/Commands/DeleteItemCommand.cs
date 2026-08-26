using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Abstractions.Time;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Items.Commands;

/// <summary>Logically removes an item from the bank.</summary>
/// <param name="ItemId">The item to delete.</param>
[RequiresPermission(Permissions.ItemsDelete)]
public sealed record DeleteItemCommand(Guid ItemId) : ICommand<Result>;

/// <summary>Validates <see cref="DeleteItemCommand"/>.</summary>
public sealed class DeleteItemCommandValidator : AbstractValidator<DeleteItemCommand>
{
    /// <summary>Initializes a new instance of the <see cref="DeleteItemCommandValidator"/> class.</summary>
    public DeleteItemCommandValidator() => RuleFor(command => command.ItemId).NotEmpty();
}

/// <summary>Handles <see cref="DeleteItemCommand"/>.</summary>
/// <param name="items">The item repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
/// <param name="clock">The clock supplying the deletion instant.</param>
internal sealed class DeleteItemCommandHandler(
    IItemRepository items,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<DeleteItemCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(DeleteItemCommand request, CancellationToken cancellationToken)
    {
        var item = await items.GetAsync(new ItemId(request.ItemId), cancellationToken);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("item.not_found", "The item does not exist."));
        }

        if (!ItemOwnershipPolicy.CanEdit(item, currentUser))
        {
            return Result.Failure(Error.Forbidden(
                "item.not_owner",
                "Only the author of an item, or an administrator, may delete it."));
        }

        item.Delete(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
