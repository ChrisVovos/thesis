using ItemAuthoring.Domain.Common;
using ItemAuthoring.Domain.Items;
using ItemAuthoring.Domain.Items.Events;
using Shouldly;

namespace ItemAuthoring.Domain.Tests.Items;

public sealed class ItemLifecycleTests
{
    [Fact]
    public void A_new_item_starts_as_a_draft_and_announces_its_creation()
    {
        var item = TestItems.SingleResponse();

        item.Status.ShouldBe(ItemStatus.Draft);
        item.VersionNumber.ShouldBe(0);
        item.DomainEvents.OfType<ItemCreatedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void A_draft_can_be_submitted_approved_and_published()
    {
        var item = TestItems.SingleResponse();

        item.SubmitForReview();
        item.Status.ShouldBe(ItemStatus.InReview);

        item.Approve();
        item.Status.ShouldBe(ItemStatus.Approved);

        item.Publish(DateTimeOffset.UtcNow);
        item.Status.ShouldBe(ItemStatus.Published);
        item.VersionNumber.ShouldBe(1);
        item.Versions.ShouldHaveSingleItem();
    }

    [Fact]
    public void A_draft_cannot_be_approved_without_review()
    {
        var item = TestItems.SingleResponse();

        var exception = Should.Throw<DomainException>(() => item.Approve());

        exception.Code.ShouldBe("item.invalid_transition");
    }

    [Fact]
    public void An_unapproved_item_cannot_be_published()
    {
        var item = TestItems.SingleResponse();
        item.SubmitForReview();

        Should.Throw<DomainException>(() => item.Publish(DateTimeOffset.UtcNow))
            .Code.ShouldBe("item.invalid_transition");
    }

    [Fact]
    public void Publishing_a_revised_item_creates_a_second_immutable_version()
    {
        var item = (SingleResponseItem)TestItems.Published(TestItems.SingleResponse());

        item.ReturnToDraft();
        item.UpdateDetails(
            ItemStem.Create("Which of the following is the smallest prime number?"),
            DifficultyLevel.VeryEasy,
            TestItems.Category,
            Points.Create(1m));
        item.SubmitForReview();
        item.Approve();
        item.Publish(DateTimeOffset.UtcNow);

        item.VersionNumber.ShouldBe(2);
        item.Versions.Count.ShouldBe(2);
        item.Versions.Select(version => version.StemText).ShouldContain(
            "Which of the following is a prime number?");
    }

    [Fact]
    public void A_published_item_cannot_be_edited_until_it_returns_to_draft()
    {
        var item = TestItems.Published(TestItems.SingleResponse());

        Should.Throw<DomainException>(() => item.UpdateDetails(
                ItemStem.Create("A different prompt."),
                DifficultyLevel.Hard,
                TestItems.Category,
                Points.Create(3m)))
            .Code.ShouldBe("item.not_editable");
    }

    [Fact]
    public void A_published_item_must_be_retired_before_it_can_be_deleted()
    {
        var item = TestItems.Published(TestItems.SingleResponse());

        Should.Throw<DomainException>(() => item.Delete(DateTimeOffset.UtcNow))
            .Code.ShouldBe("item.delete_published");

        item.Retire();
        item.Delete(DateTimeOffset.UtcNow);
        item.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void Deleting_twice_is_idempotent()
    {
        var item = TestItems.SingleResponse();
        var deletedAt = DateTimeOffset.UtcNow;

        item.Delete(deletedAt);
        item.Delete(deletedAt.AddHours(1));

        item.DeletedAtUtc.ShouldBe(deletedAt);
    }

    [Fact]
    public void A_deleted_item_cannot_be_transitioned()
    {
        var item = TestItems.SingleResponse();
        item.Delete(DateTimeOffset.UtcNow);

        Should.Throw<DomainException>(item.SubmitForReview).Code.ShouldBe("item.deleted");
    }

    [Fact]
    public void Every_transition_announces_the_states_it_moved_between()
    {
        var item = TestItems.SingleResponse();
        item.ClearDomainEvents();

        item.SubmitForReview();

        var raised = item.DomainEvents.OfType<ItemStatusChangedDomainEvent>().ShouldHaveSingleItem();
        raised.From.ShouldBe(ItemStatus.Draft);
        raised.To.ShouldBe(ItemStatus.InReview);
    }

    [Fact]
    public void Transitioning_to_the_current_state_is_a_no_operation()
    {
        var item = TestItems.SingleResponse();
        item.ClearDomainEvents();

        item.ReturnToDraft();

        item.Status.ShouldBe(ItemStatus.Draft);
        item.DomainEvents.ShouldBeEmpty();
    }
}
