using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Items;
using ItemAuthoring.Application.Items.Commands;
using ItemAuthoring.Application.Tests.TestDoubles;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;
using NSubstitute;
using Shouldly;

namespace ItemAuthoring.Application.Tests.Items;

public sealed class ItemCommandHandlerTests
{
    private readonly IItemRepository _items = Substitute.For<IItemRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly ITagRepository _tags = Substitute.For<ITagRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedClock _clock = FixedClock.At(2026, 8, 24);
    private readonly FakeCurrentUser _author = FakeCurrentUser.With(
        Permissions.ItemsUpdate,
        Permissions.ItemsDelete);

    private SingleResponseItem AuthoredItem()
    {
        var item = SingleResponseItem.Create(
            ItemStem.Create("Which of the following is a prime number?"),
            DifficultyLevel.Easy,
            CategoryId.New(),
            Points.Create(1m),
            _author.UserId!.Value,
            [
                ItemOption.Create("7", true, 0),
                ItemOption.Create("8", false, 1),
            ]);

        _items.GetAsync(Arg.Any<ItemId>(), Arg.Any<CancellationToken>()).Returns(item);
        return item;
    }

    private static UpdateItemCommand UpdateCommand(Guid itemId, Guid categoryId) => new(
        itemId,
        "Which of the following is the smallest prime number?",
        DifficultyLevel.VeryEasy,
        categoryId,
        2m,
        [
            new ItemOptionInput("2", true),
            new ItemOptionInput("3", false),
        ]);

    [Fact]
    public async Task An_author_may_edit_their_own_draft()
    {
        var item = AuthoredItem();
        _categories.IsActiveAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>()).Returns(true);
        _tags.FindMissingAsync(Arg.Any<IReadOnlyCollection<TagId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new UpdateItemCommandHandler(
            _items, _categories, _tags, _unitOfWork, _author);

        var result = await handler.HandleAsync(
            UpdateCommand(item.Id.Value, CategoryId.New().Value),
            default);

        result.IsSuccess.ShouldBeTrue();
        item.Stem.Text.ShouldBe("Which of the following is the smallest prime number?");
        item.Options.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Another_author_cannot_edit_someone_elses_item()
    {
        var item = AuthoredItem();
        var stranger = FakeCurrentUser.With(Permissions.ItemsUpdate);
        var handler = new UpdateItemCommandHandler(
            _items, _categories, _tags, _unitOfWork, stranger);

        var result = await handler.HandleAsync(
            UpdateCommand(item.Id.Value, CategoryId.New().Value),
            default);

        result.Error.Code.ShouldBe("item.not_owner");
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Editing_a_missing_item_is_reported_as_not_found()
    {
        _items.GetAsync(Arg.Any<ItemId>(), Arg.Any<CancellationToken>()).Returns((Item?)null);
        var handler = new UpdateItemCommandHandler(
            _items, _categories, _tags, _unitOfWork, _author);

        var result = await handler.HandleAsync(
            UpdateCommand(Guid.CreateVersion7(), Guid.CreateVersion7()),
            default);

        result.Error.Code.ShouldBe("item.not_found");
    }

    [Fact]
    public async Task Deleting_an_item_records_the_deletion_instant()
    {
        var item = AuthoredItem();
        var handler = new DeleteItemCommandHandler(_items, _unitOfWork, _author, _clock);

        var result = await handler.HandleAsync(new DeleteItemCommand(item.Id.Value), default);

        result.IsSuccess.ShouldBeTrue();
        item.IsDeleted.ShouldBeTrue();
        item.DeletedAtUtc.ShouldBe(_clock.UtcNow);
    }

    [Fact]
    public async Task Every_lifecycle_transition_reaches_the_aggregate()
    {
        var item = AuthoredItem();

        await new SubmitItemForReviewCommandHandler(_items, _unitOfWork)
            .HandleAsync(new SubmitItemForReviewCommand(item.Id.Value), default);
        item.Status.ShouldBe(ItemStatus.InReview);

        await new ApproveItemCommandHandler(_items, _unitOfWork)
            .HandleAsync(new ApproveItemCommand(item.Id.Value), default);
        item.Status.ShouldBe(ItemStatus.Approved);

        await new PublishItemCommandHandler(_items, _unitOfWork, _clock)
            .HandleAsync(new PublishItemCommand(item.Id.Value), default);
        item.Status.ShouldBe(ItemStatus.Published);
        item.Versions.ShouldHaveSingleItem().PublishedAtUtc.ShouldBe(_clock.UtcNow);

        await new RetireItemCommandHandler(_items, _unitOfWork)
            .HandleAsync(new RetireItemCommand(item.Id.Value), default);
        item.Status.ShouldBe(ItemStatus.Retired);

        await new ReturnItemToDraftCommandHandler(_items, _unitOfWork)
            .HandleAsync(new ReturnItemToDraftCommand(item.Id.Value), default);
        item.Status.ShouldBe(ItemStatus.Draft);
    }

    [Fact]
    public async Task A_transition_on_a_missing_item_is_reported_as_not_found()
    {
        _items.GetAsync(Arg.Any<ItemId>(), Arg.Any<CancellationToken>()).Returns((Item?)null);

        var result = await new ApproveItemCommandHandler(_items, _unitOfWork)
            .HandleAsync(new ApproveItemCommand(Guid.CreateVersion7()), default);

        result.Error.Code.ShouldBe("item.not_found");
    }

    [Fact]
    public async Task Creating_a_tag_that_already_exists_returns_the_existing_one()
    {
        var existing = Tag.Create(TagName.Create("algebra"));
        _tags.GetByNormalizedNamesAsync(
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns([existing]);

        var result = await new CreateTagCommandHandler(_tags, _unitOfWork)
            .HandleAsync(new CreateTagCommand("ALGEBRA"), default);

        result.Value.ShouldBe(existing.Id.Value);
        _tags.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task A_category_that_still_holds_items_cannot_be_deleted()
    {
        var category = Category.Create(CategoryName.Create("Mathematics"));
        _categories.GetAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>()).Returns(category);
        _categories.HasItemsAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await new DeleteCategoryCommandHandler(_categories, _unitOfWork)
            .HandleAsync(new DeleteCategoryCommand(category.Id.Value), default);

        result.Error.Code.ShouldBe("category.in_use");
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    [Fact]
    public async Task A_sibling_category_name_must_be_unique()
    {
        _categories.NameExistsAsync(
                Arg.Any<string>(),
                Arg.Any<CategoryId?>(),
                Arg.Any<CategoryId?>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await new CreateCategoryCommandHandler(_categories, _unitOfWork)
            .HandleAsync(new CreateCategoryCommand("Mathematics", null, null), default);

        result.Error.Code.ShouldBe("category.name_taken");
    }

    [Fact]
    public void The_ownership_policy_admits_the_author_and_the_administrator_only()
    {
        var item = AuthoredItem();

        ItemOwnershipPolicy.CanEdit(item, _author).ShouldBeTrue();
        ItemOwnershipPolicy.CanEdit(item, FakeCurrentUser.Administrator()).ShouldBeTrue();
        ItemOwnershipPolicy.CanEdit(item, FakeCurrentUser.With(Permissions.ItemsUpdate))
            .ShouldBeFalse();
    }
}
