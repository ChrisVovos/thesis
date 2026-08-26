using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Exams;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Exams.Commands;

/// <summary>Appends a section to a draft exam.</summary>
/// <param name="ExamId">The exam to change.</param>
/// <param name="Title">The section title.</param>
/// <param name="Instructions">Optional candidate instructions.</param>
[RequiresPermission(Permissions.ExamsUpdate)]
public sealed record AddExamSectionCommand(Guid ExamId, string Title, string? Instructions)
    : ICommand<Result<Guid>>;

/// <summary>Replaces the editorial details of a section.</summary>
/// <param name="ExamId">The exam to change.</param>
/// <param name="SectionId">The section to update.</param>
/// <param name="Title">The new title.</param>
/// <param name="Instructions">The new instructions.</param>
[RequiresPermission(Permissions.ExamsUpdate)]
public sealed record UpdateExamSectionCommand(
    Guid ExamId,
    Guid SectionId,
    string Title,
    string? Instructions) : ICommand<Result>;

/// <summary>Removes a section together with all of its placements.</summary>
/// <param name="ExamId">The exam to change.</param>
/// <param name="SectionId">The section to remove.</param>
[RequiresPermission(Permissions.ExamsUpdate)]
public sealed record RemoveExamSectionCommand(Guid ExamId, Guid SectionId) : ICommand<Result>;

/// <summary>Reorders the sections of an exam.</summary>
/// <param name="ExamId">The exam to change.</param>
/// <param name="OrderedSectionIds">Every section of the exam, in the desired order.</param>
[RequiresPermission(Permissions.ExamsUpdate)]
public sealed record ReorderExamSectionsCommand(Guid ExamId, IReadOnlyList<Guid> OrderedSectionIds)
    : ICommand<Result>;

/// <summary>Validates <see cref="AddExamSectionCommand"/>.</summary>
public sealed class AddExamSectionCommandValidator : AbstractValidator<AddExamSectionCommand>
{
    /// <summary>Initializes a new instance of the <see cref="AddExamSectionCommandValidator"/> class.</summary>
    public AddExamSectionCommandValidator()
    {
        RuleFor(command => command.ExamId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(ExamSection.MaxTitleLength);
        RuleFor(command => command.Instructions).MaximumLength(ExamSection.MaxInstructionsLength);
    }
}

/// <summary>Validates <see cref="UpdateExamSectionCommand"/>.</summary>
public sealed class UpdateExamSectionCommandValidator : AbstractValidator<UpdateExamSectionCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateExamSectionCommandValidator"/> class.</summary>
    public UpdateExamSectionCommandValidator()
    {
        RuleFor(command => command.ExamId).NotEmpty();
        RuleFor(command => command.SectionId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(ExamSection.MaxTitleLength);
        RuleFor(command => command.Instructions).MaximumLength(ExamSection.MaxInstructionsLength);
    }
}

/// <summary>Validates <see cref="ReorderExamSectionsCommand"/>.</summary>
public sealed class ReorderExamSectionsCommandValidator
    : AbstractValidator<ReorderExamSectionsCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ReorderExamSectionsCommandValidator"/> class.</summary>
    public ReorderExamSectionsCommandValidator()
    {
        RuleFor(command => command.ExamId).NotEmpty();
        RuleFor(command => command.OrderedSectionIds).NotEmpty();
    }
}

/// <summary>Handles <see cref="AddExamSectionCommand"/>.</summary>
/// <param name="exams">The exam repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class AddExamSectionCommandHandler(
    IExamRepository exams,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<AddExamSectionCommand, Result<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(
        AddExamSectionCommand request,
        CancellationToken cancellationToken)
    {
        var exam = await exams.GetAsync(new ExamId(request.ExamId), cancellationToken);
        var guard = ExamOwnershipPolicy.Authorize(exam, currentUser);
        if (guard.IsFailure)
        {
            return Result.Failure<Guid>(guard.Error);
        }

        var section = exam!.AddSection(request.Title, request.Instructions);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(section.Id.Value);
    }
}

/// <summary>Handles <see cref="UpdateExamSectionCommand"/>.</summary>
/// <param name="exams">The exam repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class UpdateExamSectionCommandHandler(
    IExamRepository exams,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateExamSectionCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        UpdateExamSectionCommand request,
        CancellationToken cancellationToken)
    {
        var exam = await exams.GetAsync(new ExamId(request.ExamId), cancellationToken);
        var guard = ExamOwnershipPolicy.Authorize(exam, currentUser);
        if (guard.IsFailure)
        {
            return guard;
        }

        exam!.UpdateSection(
            new ExamSectionId(request.SectionId),
            request.Title,
            request.Instructions);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="RemoveExamSectionCommand"/>.</summary>
/// <param name="exams">The exam repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class RemoveExamSectionCommandHandler(
    IExamRepository exams,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<RemoveExamSectionCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        RemoveExamSectionCommand request,
        CancellationToken cancellationToken)
    {
        var exam = await exams.GetAsync(new ExamId(request.ExamId), cancellationToken);
        var guard = ExamOwnershipPolicy.Authorize(exam, currentUser);
        if (guard.IsFailure)
        {
            return guard;
        }

        exam!.RemoveSection(new ExamSectionId(request.SectionId));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="ReorderExamSectionsCommand"/>.</summary>
/// <param name="exams">The exam repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class ReorderExamSectionsCommandHandler(
    IExamRepository exams,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<ReorderExamSectionsCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        ReorderExamSectionsCommand request,
        CancellationToken cancellationToken)
    {
        var exam = await exams.GetAsync(new ExamId(request.ExamId), cancellationToken);
        var guard = ExamOwnershipPolicy.Authorize(exam, currentUser);
        if (guard.IsFailure)
        {
            return guard;
        }

        exam!.ReorderSections(
            request.OrderedSectionIds.Select(id => new ExamSectionId(id)).ToList());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
