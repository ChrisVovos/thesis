using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Exams;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Exams.Commands;

/// <summary>Places an existing bank item into a section of a draft exam.</summary>
/// <param name="ExamId">The exam to change.</param>
/// <param name="SectionId">The section to append to.</param>
/// <param name="ItemId">The bank item to place.</param>
/// <param name="ScoreOverride">An optional exam specific score.</param>
[RequiresPermission(Permissions.ExamsUpdate)]
public sealed record AddExamItemCommand(
    Guid ExamId,
    Guid SectionId,
    Guid ItemId,
    decimal? ScoreOverride = null) : ICommand<Result<Guid>>;

/// <summary>Removes a placement from a section.</summary>
/// <param name="ExamId">The exam to change.</param>
/// <param name="SectionId">The section holding the placement.</param>
/// <param name="ExamItemId">The placement to remove.</param>
[RequiresPermission(Permissions.ExamsUpdate)]
public sealed record RemoveExamItemCommand(Guid ExamId, Guid SectionId, Guid ExamItemId)
    : ICommand<Result>;

/// <summary>Reorders the placements inside a section.</summary>
/// <param name="ExamId">The exam to change.</param>
/// <param name="SectionId">The section to reorder.</param>
/// <param name="OrderedExamItemIds">Every placement of the section, in the desired order.</param>
[RequiresPermission(Permissions.ExamsUpdate)]
public sealed record ReorderExamItemsCommand(
    Guid ExamId,
    Guid SectionId,
    IReadOnlyList<Guid> OrderedExamItemIds) : ICommand<Result>;

/// <summary>Validates <see cref="AddExamItemCommand"/>.</summary>
public sealed class AddExamItemCommandValidator : AbstractValidator<AddExamItemCommand>
{
    /// <summary>Initializes a new instance of the <see cref="AddExamItemCommandValidator"/> class.</summary>
    public AddExamItemCommandValidator()
    {
        RuleFor(command => command.ExamId).NotEmpty();
        RuleFor(command => command.SectionId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
        RuleFor(command => command.ScoreOverride)
            .GreaterThan(0m)
            .LessThanOrEqualTo(Points.MaxValue)
            .When(command => command.ScoreOverride is not null);
    }
}

/// <summary>Validates <see cref="ReorderExamItemsCommand"/>.</summary>
public sealed class ReorderExamItemsCommandValidator : AbstractValidator<ReorderExamItemsCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ReorderExamItemsCommandValidator"/> class.</summary>
    public ReorderExamItemsCommandValidator()
    {
        RuleFor(command => command.ExamId).NotEmpty();
        RuleFor(command => command.SectionId).NotEmpty();
        RuleFor(command => command.OrderedExamItemIds).NotEmpty();
    }
}

/// <summary>Handles <see cref="AddExamItemCommand"/>.</summary>
/// <remarks>
/// Only published items may be placed. An exam assembled from drafts would silently change whenever
/// an author edited one of them, which is precisely the failure mode item versioning exists to
/// prevent. The check lives here because it spans two aggregates and belongs to neither.
/// </remarks>
/// <param name="exams">The exam repository.</param>
/// <param name="items">The item repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class AddExamItemCommandHandler(
    IExamRepository exams,
    IItemRepository items,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<AddExamItemCommand, Result<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(
        AddExamItemCommand request,
        CancellationToken cancellationToken)
    {
        var exam = await exams.GetAsync(new ExamId(request.ExamId), cancellationToken);
        var guard = ExamOwnershipPolicy.Authorize(exam, currentUser);
        if (guard.IsFailure)
        {
            return Result.Failure<Guid>(guard.Error);
        }

        var status = await items.GetStatusAsync(new ItemId(request.ItemId), cancellationToken);
        if (status is null)
        {
            return Result.Failure<Guid>(Error.NotFound("item.not_found", "The item does not exist."));
        }

        if (status is not ItemStatus.Published)
        {
            return Result.Failure<Guid>(Error.Conflict(
                "exam.item_not_published",
                "Only published items can be added to an exam."));
        }

        var placement = exam!.AddItem(
            new ExamSectionId(request.SectionId),
            new ItemId(request.ItemId),
            request.ScoreOverride is { } score ? Points.Create(score) : null);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(placement.Id.Value);
    }
}

/// <summary>Handles <see cref="RemoveExamItemCommand"/>.</summary>
/// <param name="exams">The exam repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class RemoveExamItemCommandHandler(
    IExamRepository exams,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<RemoveExamItemCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        RemoveExamItemCommand request,
        CancellationToken cancellationToken)
    {
        var exam = await exams.GetAsync(new ExamId(request.ExamId), cancellationToken);
        var guard = ExamOwnershipPolicy.Authorize(exam, currentUser);
        if (guard.IsFailure)
        {
            return guard;
        }

        exam!.RemoveItem(new ExamSectionId(request.SectionId), new ExamItemId(request.ExamItemId));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="ReorderExamItemsCommand"/>.</summary>
/// <param name="exams">The exam repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class ReorderExamItemsCommandHandler(
    IExamRepository exams,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<ReorderExamItemsCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        ReorderExamItemsCommand request,
        CancellationToken cancellationToken)
    {
        var exam = await exams.GetAsync(new ExamId(request.ExamId), cancellationToken);
        var guard = ExamOwnershipPolicy.Authorize(exam, currentUser);
        if (guard.IsFailure)
        {
            return guard;
        }

        exam!.ReorderItems(
            new ExamSectionId(request.SectionId),
            request.OrderedExamItemIds.Select(id => new ExamItemId(id)).ToList());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
