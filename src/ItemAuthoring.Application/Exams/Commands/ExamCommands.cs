using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Abstractions.Time;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Exams;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Exams.Commands;

/// <summary>Creates a draft exam.</summary>
/// <param name="Title">The exam title.</param>
/// <param name="Description">An optional description.</param>
/// <param name="TimeLimitMinutes">An optional delivery time limit in minutes.</param>
/// <param name="PassingScorePercentage">The percentage of the total score required to pass.</param>
[RequiresPermission(Permissions.ExamsCreate)]
public sealed record CreateExamCommand(
    string Title,
    string? Description,
    int? TimeLimitMinutes,
    int PassingScorePercentage) : ICommand<Result<Guid>>;

/// <summary>Replaces the editorial details of a draft exam.</summary>
/// <param name="ExamId">The exam to update.</param>
/// <param name="Title">The new title.</param>
/// <param name="Description">The new description.</param>
/// <param name="TimeLimitMinutes">The new delivery time limit in minutes.</param>
/// <param name="PassingScorePercentage">The new passing score percentage.</param>
[RequiresPermission(Permissions.ExamsUpdate)]
public sealed record UpdateExamCommand(
    Guid ExamId,
    string Title,
    string? Description,
    int? TimeLimitMinutes,
    int PassingScorePercentage) : ICommand<Result>;

/// <summary>Logically removes an exam.</summary>
/// <param name="ExamId">The exam to delete.</param>
[RequiresPermission(Permissions.ExamsDelete)]
public sealed record DeleteExamCommand(Guid ExamId) : ICommand<Result>;

/// <summary>Validates <see cref="CreateExamCommand"/>.</summary>
public sealed class CreateExamCommandValidator : AbstractValidator<CreateExamCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateExamCommandValidator"/> class.</summary>
    public CreateExamCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(ExamTitle.MaxLength);
        RuleFor(command => command.Description).MaximumLength(Exam.MaxDescriptionLength);
        RuleFor(command => command.PassingScorePercentage).InclusiveBetween(0, 100);
        RuleFor(command => command.TimeLimitMinutes)
            .InclusiveBetween(1, 1440)
            .When(command => command.TimeLimitMinutes is not null);
    }
}

/// <summary>Validates <see cref="UpdateExamCommand"/>.</summary>
public sealed class UpdateExamCommandValidator : AbstractValidator<UpdateExamCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateExamCommandValidator"/> class.</summary>
    public UpdateExamCommandValidator()
    {
        RuleFor(command => command.ExamId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(ExamTitle.MaxLength);
        RuleFor(command => command.Description).MaximumLength(Exam.MaxDescriptionLength);
        RuleFor(command => command.PassingScorePercentage).InclusiveBetween(0, 100);
        RuleFor(command => command.TimeLimitMinutes)
            .InclusiveBetween(1, 1440)
            .When(command => command.TimeLimitMinutes is not null);
    }
}

/// <summary>Handles <see cref="CreateExamCommand"/>.</summary>
/// <param name="exams">The exam repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class CreateExamCommandHandler(
    IExamRepository exams,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<CreateExamCommand, Result<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(
        CreateExamCommand request,
        CancellationToken cancellationToken)
    {
        var exam = Exam.Create(
            ExamTitle.Create(request.Title),
            request.Description,
            request.TimeLimitMinutes,
            request.PassingScorePercentage,
            currentUser.UserId!.Value);

        exams.Add(exam);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(exam.Id.Value);
    }
}

/// <summary>Handles <see cref="UpdateExamCommand"/>.</summary>
/// <param name="exams">The exam repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class UpdateExamCommandHandler(
    IExamRepository exams,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateExamCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(UpdateExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await exams.GetAsync(new ExamId(request.ExamId), cancellationToken);
        var guard = ExamOwnershipPolicy.Authorize(exam, currentUser);
        if (guard.IsFailure)
        {
            return guard;
        }

        exam!.UpdateDetails(
            ExamTitle.Create(request.Title),
            request.Description,
            request.TimeLimitMinutes,
            request.PassingScorePercentage);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="DeleteExamCommand"/>.</summary>
/// <param name="exams">The exam repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
/// <param name="clock">The clock supplying the deletion instant.</param>
internal sealed class DeleteExamCommandHandler(
    IExamRepository exams,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : IRequestHandler<DeleteExamCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(DeleteExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await exams.GetAsync(new ExamId(request.ExamId), cancellationToken);
        var guard = ExamOwnershipPolicy.Authorize(exam, currentUser);
        if (guard.IsFailure)
        {
            return guard;
        }

        exam!.Delete(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
