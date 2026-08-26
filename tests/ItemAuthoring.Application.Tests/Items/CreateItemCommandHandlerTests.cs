using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Items.Commands;
using ItemAuthoring.Application.Tests.TestDoubles;
using ItemAuthoring.Domain.Items;
using NSubstitute;
using Shouldly;

namespace ItemAuthoring.Application.Tests.Items;

public sealed class CreateItemCommandHandlerTests
{
    private readonly IItemRepository _items = Substitute.For<IItemRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly ITagRepository _tags = Substitute.For<ITagRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeCurrentUser _currentUser = FakeCurrentUser.Administrator();

    private CreateItemCommandHandler CreateHandler()
        => new(_items, _categories, _tags, _unitOfWork, _currentUser);

    private static CreateItemCommand ValidCommand(Guid categoryId) => new(
        ItemType.MultipleChoiceSingleResponse,
        "Which of the following is a prime number?",
        DifficultyLevel.Easy,
        categoryId,
        1m,
        [
            new ItemOptionInput("7", true),
            new ItemOptionInput("8", false),
        ]);

    [Fact]
    public async Task An_item_is_persisted_when_the_category_and_tags_exist()
    {
        var categoryId = Guid.CreateVersion7();
        _categories.IsActiveAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _tags.FindMissingAsync(Arg.Any<IReadOnlyCollection<TagId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await CreateHandler().HandleAsync(ValidCommand(categoryId), default);

        result.IsSuccess.ShouldBeTrue();
        _items.Received(1).Add(Arg.Is<Item>(item =>
            item.Type == ItemType.MultipleChoiceSingleResponse));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_inactive_or_missing_category_is_reported_as_not_found()
    {
        _categories.IsActiveAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().HandleAsync(ValidCommand(Guid.CreateVersion7()), default);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("category.not_found");
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _items.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task An_unknown_tag_prevents_creation()
    {
        _categories.IsActiveAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _tags.FindMissingAsync(Arg.Any<IReadOnlyCollection<TagId>>(), Arg.Any<CancellationToken>())
            .Returns([TagId.New()]);

        var command = ValidCommand(Guid.CreateVersion7()) with { TagIds = [Guid.CreateVersion7()] };
        var result = await CreateHandler().HandleAsync(command, default);

        result.Error.Code.ShouldBe("tag.not_found");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task An_essay_command_produces_an_essay_aggregate()
    {
        _categories.IsActiveAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _tags.FindMissingAsync(Arg.Any<IReadOnlyCollection<TagId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var command = new CreateItemCommand(
            ItemType.Essay,
            "Explain why there are infinitely many prime numbers.",
            DifficultyLevel.Hard,
            Guid.CreateVersion7(),
            10m,
            Rubric: new EssayRubricInput("Award marks for a proof sketch.", 100, 400));

        var result = await CreateHandler().HandleAsync(command, default);

        result.IsSuccess.ShouldBeTrue();
        _items.Received(1).Add(Arg.Any<EssayItem>());
    }

    [Fact]
    public async Task An_either_or_command_produces_a_two_option_aggregate()
    {
        _categories.IsActiveAsync(Arg.Any<CategoryId>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _tags.FindMissingAsync(Arg.Any<IReadOnlyCollection<TagId>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var command = new CreateItemCommand(
            ItemType.EitherOr,
            "Every prime greater than two is odd.",
            DifficultyLevel.Easy,
            Guid.CreateVersion7(),
            1m,
            [
                new ItemOptionInput("True", true),
                new ItemOptionInput("False", false),
            ]);

        var result = await CreateHandler().HandleAsync(command, default);

        result.IsSuccess.ShouldBeTrue();
        _items.Received(1).Add(Arg.Is<EitherOrItem>(item => item.Options.Count == 2));
    }
}
