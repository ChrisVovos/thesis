using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Abstractions.Time;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Exams;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Exams.Commands;

/// <summary>Freezes a draft exam for delivery.</summary>
/// <param name="ExamId">The exam to publish.</param>
[RequiresPermission(Permissions.ExamsPublish)]
public sealed record PublishExamCommand(Guid ExamId) : ICommand<Result>;

/// <summary>Withdraws a published exam from delivery.</summary>
/// <param name="ExamId">The exam to archive.</param>
[RequiresPermission(Permissions.ExamsPublish)]
public sealed record ArchiveExamCommand(Guid ExamId) : ICommand<Result>;

/// <summary>Returns a published or archived exam to draft.</summary>
/// <param name="ExamId">The exam to reopen.</param>
[RequiresPermission(Permissions.ExamsPublish)]
public sealed record ReturnExamToDraftCommand(Guid ExamId) : ICommand<Result>;

/// <summary>Handles <see cref="PublishExamCommand"/>.</summary>
/// <remarks>
/// This is one of the two use cases that genuinely spans aggregates, and therefore one of the two
/// that opens an explicit transaction. Publication must observe a consistent snapshot of every
/// referenced item: without the transaction, an item could be retired between the moment its status
/// is verified and the moment the exam is frozen, producing a published exam that references
/// withdrawn content.
/// </remarks>
/// <param name="exams">The exam repository.</param>
/// <param name="items">The item repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
/// <param name="clock">The clock supplying the publication instant.</param>
internal sealed class PublishExamCommandHandler(
    IExamRepository exams,
    IItemRepository items,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<PublishExamCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(PublishExamCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var exam = await exams.GetAsync(new ExamId(request.ExamId), cancellationToken);
        var guard = ExamOwnershipPolicy.Authorize(exam, currentUser);
        if (guard.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return guard;
        }

        var referenced = exam!.Sections
            .SelectMany(section => section.Items)
            .Select(placement => placement.ItemId)
            .Distinct()
            .ToList();

        var scores = await items.GetMaximumScoresAsync(referenced, cancellationToken);
        if (scores.Count != referenced.Count)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(Error.Conflict(
                "exam.item_missing",
                "The exam references an item that no longer exists."));
        }

        exam.Publish(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="ArchiveExamCommand"/>.</summary>
/// <param name="exams">The exam repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class ArchiveExamCommandHandler(
    IExamRepository exams,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<ArchiveExamCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(ArchiveExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await exams.GetAsync(new ExamId(request.ExamId), cancellationToken);
        var guard = ExamOwnershipPolicy.Authorize(exam, currentUser);
        if (guard.IsFailure)
        {
            return guard;
        }

        exam!.Archive();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="ReturnExamToDraftCommand"/>.</summary>
/// <param name="exams">The exam repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class ReturnExamToDraftCommandHandler(
    IExamRepository exams,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<ReturnExamToDraftCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        ReturnExamToDraftCommand request,
        CancellationToken cancellationToken)
    {
        var exam = await exams.GetAsync(new ExamId(request.ExamId), cancellationToken);
        var guard = ExamOwnershipPolicy.Authorize(exam, currentUser);
        if (guard.IsFailure)
        {
            return guard;
        }

        exam!.ReturnToDraft();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
