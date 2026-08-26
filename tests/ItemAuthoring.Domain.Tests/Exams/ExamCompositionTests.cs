using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Exams;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;
using Shouldly;

namespace ItemAuthoring.Domain.Tests.Exams;

public sealed class ExamCompositionTests
{
    private static readonly UserId Owner = UserId.New();

    private static Exam NewExam()
        => Exam.Create(ExamTitle.Create("Discrete Mathematics — Midterm"), null, 90, 60, Owner);

    [Fact]
    public void A_new_exam_starts_as_an_empty_draft()
    {
        var exam = NewExam();

        exam.Status.ShouldBe(ExamStatus.Draft);
        exam.Sections.ShouldBeEmpty();
        exam.ValidateComposition().ShouldContain("exam.no_sections");
    }

    [Fact]
    public void Sections_and_items_are_appended_in_order()
    {
        var exam = NewExam();
        var first = exam.AddSection("Part A");
        var second = exam.AddSection("Part B");

        first.Position.ShouldBe(0);
        second.Position.ShouldBe(1);

        var itemA = exam.AddItem(first.Id, ItemId.New());
        var itemB = exam.AddItem(first.Id, ItemId.New());

        itemA.Position.ShouldBe(0);
        itemB.Position.ShouldBe(1);
    }

    [Fact]
    public void The_same_bank_item_cannot_appear_twice_in_one_exam()
    {
        var exam = NewExam();
        var sectionA = exam.AddSection("Part A");
        var sectionB = exam.AddSection("Part B");
        var itemId = ItemId.New();
        exam.AddItem(sectionA.Id, itemId);

        Should.Throw<DomainException>(() => exam.AddItem(sectionB.Id, itemId))
            .Code.ShouldBe("exam.duplicate_item");
    }

    [Fact]
    public void Removing_an_item_closes_the_gap_in_the_ordering()
    {
        var exam = NewExam();
        var section = exam.AddSection("Part A");
        var first = exam.AddItem(section.Id, ItemId.New());
        exam.AddItem(section.Id, ItemId.New());
        var third = exam.AddItem(section.Id, ItemId.New());

        exam.RemoveItem(section.Id, first.Id);

        section.Items.Select(item => item.Position).ShouldBe([0, 1]);
        section.Items.Last().Id.ShouldBe(third.Id);
    }

    [Fact]
    public void Reordering_must_list_every_item_exactly_once()
    {
        var exam = NewExam();
        var section = exam.AddSection("Part A");
        var first = exam.AddItem(section.Id, ItemId.New());
        exam.AddItem(section.Id, ItemId.New());

        Should.Throw<DomainException>(() => exam.ReorderItems(section.Id, [first.Id]))
            .Code.ShouldBe("exam.reorder_incomplete");
    }

    [Fact]
    public void Reordering_rewrites_the_positions()
    {
        var exam = NewExam();
        var section = exam.AddSection("Part A");
        var first = exam.AddItem(section.Id, ItemId.New());
        var second = exam.AddItem(section.Id, ItemId.New());

        exam.ReorderItems(section.Id, [second.Id, first.Id]);

        section.Items.First().Id.ShouldBe(second.Id);
        section.Items.First().Position.ShouldBe(0);
        section.Items.Last().Id.ShouldBe(first.Id);
        section.Items.Last().Position.ShouldBe(1);
    }

    [Fact]
    public void An_exam_with_an_empty_section_cannot_be_published()
    {
        var exam = NewExam();
        exam.AddSection("Part A");

        exam.ValidateComposition().ShouldContain("exam.empty_section");
        Should.Throw<DomainException>(() => exam.Publish(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void A_valid_exam_publishes_and_then_refuses_further_composition_changes()
    {
        var exam = NewExam();
        var section = exam.AddSection("Part A");
        exam.AddItem(section.Id, ItemId.New());

        exam.Publish(DateTimeOffset.UtcNow);

        exam.Status.ShouldBe(ExamStatus.Published);
        Should.Throw<DomainException>(() => exam.AddSection("Part B"))
            .Code.ShouldBe("exam.not_editable");
    }

    [Fact]
    public void The_total_score_prefers_an_override_over_the_bank_item_score()
    {
        var exam = NewExam();
        var section = exam.AddSection("Part A");
        var plainItem = ItemId.New();
        var overriddenItem = ItemId.New();
        exam.AddItem(section.Id, plainItem);
        exam.AddItem(section.Id, overriddenItem, Points.Create(5m));

        var total = exam.CalculateTotalScore(new Dictionary<ItemId, decimal>
        {
            [plainItem] = 2m,
            [overriddenItem] = 1m,
        });

        total.ShouldBe(7m);
    }

    [Fact]
    public void The_passing_score_must_be_a_percentage()
        => Should.Throw<DomainException>(() => Exam.Create(
                ExamTitle.Create("Invalid"), null, null, 140, Owner))
            .Code.ShouldBe("exam.passing_score_invalid");

    [Fact]
    public void A_published_exam_must_be_archived_before_deletion()
    {
        var exam = NewExam();
        var section = exam.AddSection("Part A");
        exam.AddItem(section.Id, ItemId.New());
        exam.Publish(DateTimeOffset.UtcNow);

        Should.Throw<DomainException>(() => exam.Delete(DateTimeOffset.UtcNow))
            .Code.ShouldBe("exam.delete_published");

        exam.Archive();
        exam.Delete(DateTimeOffset.UtcNow);
        exam.IsDeleted.ShouldBeTrue();
    }
}
