using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Items;
using Shouldly;

namespace ItemAuthoring.Domain.Tests.Items;

public sealed class ItemTypeInvariantTests
{
    [Fact]
    public void A_single_response_item_requires_exactly_one_correct_option()
    {
        Should.Throw<DomainException>(() => SingleResponseItem.Create(
                ItemStem.Create("Prompt"),
                DifficultyLevel.Easy,
                TestItems.Category,
                Points.Create(1m),
                TestItems.Author,
                [
                    TestItems.Option("A", true, 0),
                    TestItems.Option("B", true, 1),
                ]))
            .Code.ShouldBe("item.single_response_requires_one_correct");
    }

    [Fact]
    public void A_single_response_item_requires_at_least_two_options()
    {
        Should.Throw<DomainException>(() => SingleResponseItem.Create(
                ItemStem.Create("Prompt"),
                DifficultyLevel.Easy,
                TestItems.Category,
                Points.Create(1m),
                TestItems.Author,
                [TestItems.Option("A", true, 0)]))
            .Code.ShouldBe("item.too_few_options");
    }

    [Fact]
    public void A_multiple_response_item_requires_at_least_two_correct_options()
    {
        Should.Throw<DomainException>(() => MultipleResponseItem.Create(
                ItemStem.Create("Prompt"),
                DifficultyLevel.Easy,
                TestItems.Category,
                Points.Create(1m),
                TestItems.Author,
                [
                    TestItems.Option("A", true, 0),
                    TestItems.Option("B", false, 1),
                    TestItems.Option("C", false, 2),
                ]))
            .Code.ShouldBe("item.multiple_response_requires_two_correct");
    }

    [Fact]
    public void A_multiple_response_item_requires_at_least_one_distractor()
    {
        Should.Throw<DomainException>(() => MultipleResponseItem.Create(
                ItemStem.Create("Prompt"),
                DifficultyLevel.Easy,
                TestItems.Category,
                Points.Create(1m),
                TestItems.Author,
                [
                    TestItems.Option("A", true, 0),
                    TestItems.Option("B", true, 1),
                    TestItems.Option("C", true, 2),
                ]))
            .Code.ShouldBe("item.multiple_response_requires_distractor");
    }

    [Fact]
    public void An_either_or_item_has_exactly_two_options_with_one_correct()
    {
        var item = TestItems.EitherOr();

        item.Options.Count.ShouldBe(2);
        item.Options.Count(option => option.IsCorrect).ShouldBe(1);
        item.Type.ShouldBe(ItemType.EitherOr);
    }

    [Fact]
    public void An_either_or_item_rejects_a_third_option()
    {
        var item = TestItems.EitherOr();

        Should.Throw<DomainException>(() => item.ReplaceOptions(
            [
                TestItems.Option("Yes", true, 0),
                TestItems.Option("No", false, 1),
                TestItems.Option("Maybe", false, 2),
            ]))
            .Code.ShouldBe("item.either_or_requires_two_options");
    }

    [Fact]
    public void An_essay_item_carries_a_rubric_and_no_options()
    {
        var item = TestItems.Essay();

        item.Type.ShouldBe(ItemType.Essay);
        item.Rubric.MinimumWords.ShouldBe(100);
        item.ShouldNotBeOfType<ChoiceItem>();
    }

    [Fact]
    public void An_essay_rubric_requires_a_sensible_word_range()
    {
        Should.Throw<DomainException>(() => EssayRubric.Create("Guidance", 400, 100))
            .Code.ShouldBe("item.rubric_word_range_invalid");
    }

    [Fact]
    public void Replacing_options_reindexes_their_positions()
    {
        var item = TestItems.SingleResponse();

        item.ReplaceOptions(
        [
            TestItems.Option("First", false, 99),
            TestItems.Option("Second", true, 42),
        ]);

        item.Options.Select(option => option.Position).ShouldBe([0, 1]);
    }

    [Fact]
    public void Publishing_freezes_the_options_as_they_were()
    {
        var item = (SingleResponseItem)TestItems.Published(TestItems.SingleResponse());

        var version = item.Versions.Single();
        version.Options.Count.ShouldBe(3);
        version.Options.Count(option => option.IsCorrect).ShouldBe(1);
        version.MaximumScore.ShouldBe(1m);
    }

    [Fact]
    public void Publishing_an_essay_freezes_its_rubric()
    {
        var item = (EssayItem)TestItems.Published(TestItems.Essay());

        var version = item.Versions.Single();
        version.RubricMinimumWords.ShouldBe(100);
        version.RubricMaximumWords.ShouldBe(400);
        version.Options.ShouldBeEmpty();
    }
}
