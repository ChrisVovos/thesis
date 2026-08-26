using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Items.Commands;

/// <summary>Creates a tag, or returns the existing one when the label is already in use.</summary>
/// <param name="Name">The tag label.</param>
[RequiresPermission(Permissions.TaxonomyManage)]
public sealed record CreateTagCommand(string Name) : ICommand<Result<Guid>>;

/// <summary>Deletes a tag and detaches it from every item.</summary>
/// <param name="TagId">The tag to delete.</param>
[RequiresPermission(Permissions.TaxonomyManage)]
public sealed record DeleteTagCommand(Guid TagId) : ICommand<Result>;

/// <summary>Validates <see cref="CreateTagCommand"/>.</summary>
public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateTagCommandValidator"/> class.</summary>
    public CreateTagCommandValidator()
        => RuleFor(command => command.Name).NotEmpty().MaximumLength(TagName.MaxLength);
}

/// <summary>Handles <see cref="CreateTagCommand"/>.</summary>
/// <remarks>
/// Creating a tag is idempotent by label. Authors type tags freely, and rejecting a duplicate with a
/// conflict would force every client to implement a "look it up first" dance for no benefit.
/// </remarks>
/// <param name="tags">The tag repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class CreateTagCommandHandler(ITagRepository tags, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTagCommand, Result<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(
        CreateTagCommand request,
        CancellationToken cancellationToken)
    {
        var name = TagName.Create(request.Name);
        var existing = await tags.GetByNormalizedNamesAsync([name.Normalized], cancellationToken);
        if (existing.Count > 0)
        {
            return Result.Success(existing[0].Id.Value);
        }

        var tag = Tag.Create(name);
        tags.Add(tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(tag.Id.Value);
    }
}

/// <summary>Handles <see cref="DeleteTagCommand"/>.</summary>
/// <param name="tags">The tag repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class DeleteTagCommandHandler(ITagRepository tags, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteTagCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await tags.GetAsync(new TagId(request.TagId), cancellationToken);
        if (tag is null)
        {
            return Result.Failure(Error.NotFound("tag.not_found", "The tag does not exist."));
        }

        tags.Remove(tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
