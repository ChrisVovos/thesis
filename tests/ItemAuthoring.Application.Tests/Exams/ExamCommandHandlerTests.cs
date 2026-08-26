using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Exams;
using ItemAuthoring.Application.Exams.Commands;
using ItemAuthoring.Application.Tests.TestDoubles;
using ItemAuthoring.Domain.Exams;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;
using NSubstitute;
using Shouldly;

namespace ItemAuthoring.Application.Tests.Exams;

public sealed class ExamCommandHandlerTests
{
    private readonly IExamRepository _exams = Substitute.For<IExamRepository>();
    private readonly IItemRepository _items = Substitute.For<IItemRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedClock _clock = FixedClock.At(2026, 8, 24);
    private readonly FakeCurrentUser _owner = FakeCurrentUser.With(Permissions.ExamsUpdate);

    private Exam OwnedExam()
    {
        var exam = Exam.Create(
            ExamTitle.Create("Owned exam"),
            null,
            60,
            50,
            _owner.UserId!.Value);
        _exams.GetAsync(Arg.Any<ExamId>(), Arg.Any<CancellationToken>()).Returns(exam);
        return exam;
    }

    [Fact]
    public async Task An_exam_is_created_for_the_calling_instructor()
    {
        var handler = new CreateExamCommandHandler(_exams, _unitOfWork, _owner);

        var result = await handler.HandleAsync(
            new CreateExamCommand("Midterm", "A description.", 90, 60),
            default);

        result.IsSuccess.ShouldBeTrue();
        _exams.Received(1).Add(Arg.Is<Exam>(exam => exam.OwnerId == _owner.UserId));
    }

    [Fact]
    public async Task A_missing_exam_is_reported_as_not_found()
    {
        _exams.GetAsync(Arg.Any<ExamId>(), Arg.Any<CancellationToken>()).Returns((Exam?)null);
        var handler = new UpdateExamCommandHandler(_exams, _unitOfWork, _owner);

        var result = await handler.HandleAsync(
            new UpdateExamCommand(Guid.CreateVersion7(), "Renamed", null, null, 50),
            default);

        result.Error.Code.ShouldBe("exam.not_found");
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Another_instructor_cannot_change_someone_elses_exam()
    {
        OwnedExam();
        var stranger = FakeCurrentUser.With(Permissions.ExamsUpdate);
        var handler = new UpdateExamCommandHandler(_exams, _unitOfWork, stranger);

        var result = await handler.HandleAsync(
            new UpdateExamCommand(Guid.CreateVersion7(), "Renamed", null, null, 50),
            default);

        result.Error.Code.ShouldBe("exam.not_owner");
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact]
    public async Task An_administrator_may_change_any_exam()
    {
        OwnedExam();
        var administrator = FakeCurrentUser.Administrator();
        var handler = new UpdateExamCommandHandler(_exams, _unitOfWork, administrator);

        var result = await handler.HandleAsync(
            new UpdateExamCommand(Guid.CreateVersion7(), "Renamed", null, null, 50),
            default);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Only_a_published_item_can_be_placed_in_an_exam()
    {
        var exam = OwnedExam();
        var section = exam.AddSection("Part A");
        _items.GetStatusAsync(Arg.Any<ItemId>(), Arg.Any<CancellationToken>())
            .Returns(ItemStatus.Draft);

        var handler = new AddExamItemCommandHandler(_exams, _items, _unitOfWork, _owner);
        var result = await handler.HandleAsync(
            new AddExamItemCommand(exam.Id.Value, section.Id.Value, Guid.CreateVersion7()),
            default);

        result.Error.Code.ShouldBe("exam.item_not_published");
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public async Task An_unknown_item_cannot_be_placed_in_an_exam()
    {
        var exam = OwnedExam();
        var section = exam.AddSection("Part A");
        _items.GetStatusAsync(Arg.Any<ItemId>(), Arg.Any<CancellationToken>())
            .Returns((ItemStatus?)null);

        var handler = new AddExamItemCommandHandler(_exams, _items, _unitOfWork, _owner);
        var result = await handler.HandleAsync(
            new AddExamItemCommand(exam.Id.Value, section.Id.Value, Guid.CreateVersion7()),
            default);

        result.Error.Code.ShouldBe("item.not_found");
    }

    [Fact]
    public async Task A_published_item_is_placed_and_the_change_is_saved()
    {
        var exam = OwnedExam();
        var section = exam.AddSection("Part A");
        _items.GetStatusAsync(Arg.Any<ItemId>(), Arg.Any<CancellationToken>())
            .Returns(ItemStatus.Published);

        var handler = new AddExamItemCommandHandler(_exams, _items, _unitOfWork, _owner);
        var result = await handler.HandleAsync(
            new AddExamItemCommand(exam.Id.Value, section.Id.Value, Guid.CreateVersion7(), 4m),
            default);

        result.IsSuccess.ShouldBeTrue();
        section.Items.ShouldHaveSingleItem().ScoreOverride!.Value.ShouldBe(4m);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_section_is_appended_and_its_identity_returned()
    {
        var exam = OwnedExam();
        var handler = new AddExamSectionCommandHandler(_exams, _unitOfWork, _owner);

        var result = await handler.HandleAsync(
            new AddExamSectionCommand(exam.Id.Value, "Part A", "Answer everything."),
            default);

        result.IsSuccess.ShouldBeTrue();
        exam.Sections.ShouldHaveSingleItem().Id.Value.ShouldBe(result.Value);
    }

    [Fact]
    public async Task A_deleted_exam_records_the_deletion_instant()
    {
        var exam = OwnedExam();
        var handler = new DeleteExamCommandHandler(_exams, _unitOfWork, _owner, _clock);

        var result = await handler.HandleAsync(new DeleteExamCommand(exam.Id.Value), default);

        result.IsSuccess.ShouldBeTrue();
        exam.IsDeleted.ShouldBeTrue();
        exam.DeletedAtUtc.ShouldBe(_clock.UtcNow);
    }

    [Fact]
    public void The_ownership_policy_reports_a_missing_exam_before_it_reports_ownership()
    {
        var outcome = ExamOwnershipPolicy.Authorize(null, FakeCurrentUser.Administrator());

        outcome.Error.Code.ShouldBe("exam.not_found");
    }
}
