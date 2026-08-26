using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Items.Commands;

/// <summary>Creates a draft item of any supported answer shape.</summary>
/// <param name="Type">The answer shape to create.</param>
/// <param name="Stem">The prompt shown to the examinee.</param>
/// <param name="Difficulty">The cognitive demand of the item.</param>
/// <param name="CategoryId">The category the item is filed under.</param>
/// <param name="MaximumScore">The score a fully correct response is worth.</param>
/// <param name="Options">The answer options, required for every shape except essay.</param>
/// <param name="Rubric">The grading guidance, required for essay items.</param>
/// <param name="SampleAnswer">An optional exemplar answer for essay items.</param>
/// <param name="TagIds">The tags to attach to the new item.</param>
[RequiresPermission(Permissions.ItemsCreate)]
public sealed record CreateItemCommand(
    ItemType Type,
    string Stem,
    DifficultyLevel Difficulty,
    Guid CategoryId,
    decimal MaximumScore,
    IReadOnlyList<ItemOptionInput>? Options = null,
    EssayRubricInput? Rubric = null,
    string? SampleAnswer = null,
    IReadOnlyList<Guid>? TagIds = null) : ICommand<Result<Guid>>;

/// <summary>Validates <see cref="CreateItemCommand"/>.</summary>
public sealed class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateItemCommandValidator"/> class.</summary>
    public CreateItemCommandValidator()
    {
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.Difficulty).IsInEnum();
        RuleFor(command => command.Stem).NotEmpty().MaximumLength(ItemStem.MaxLength);
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.MaximumScore).GreaterThan(0m).LessThanOrEqualTo(Points.MaxValue);
        RuleFor(command => command.SampleAnswer)
            .MaximumLength(EssayItem.MaxSampleAnswerLength)
            .When(command => command.SampleAnswer is not null);

        When(command => command.Type is ItemType.Essay, () =>
        {
            RuleFor(command => command.Rubric).NotNull();
            RuleFor(command => command.Rubric!.Guidance)
                .NotEmpty()
                .MaximumLength(EssayRubric.MaxGuidanceLength)
                .When(command => command.Rubric is not null);
        });

        When(command => command.Type is not ItemType.Essay, () =>
        {
            RuleFor(command => command.Options).NotNull().Must(options => options is { Count: >= 2 })
                .WithMessage("At least two options are required for this item type.");
            RuleForEach(command => command.Options)
                .ChildRules(option => option.RuleFor(candidate => candidate.Text)
                    .NotEmpty()
                    .MaximumLength(OptionText.MaxLength))
                .When(command => command.Options is not null);
        });
    }
}

/// <summary>Handles <see cref="CreateItemCommand"/>.</summary>
/// <param name="items">The item repository.</param>
/// <param name="categories">The category repository.</param>
/// <param name="tags">The tag repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class CreateItemCommandHandler(
    IItemRepository items,
    ICategoryRepository categories,
    ITagRepository tags,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<CreateItemCommand, Result<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(
        CreateItemCommand request,
        CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId);
        if (!await categories.IsActiveAsync(categoryId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound(
                "category.not_found",
                "The category does not exist or no longer accepts new items."));
        }

        var tagIds = (request.TagIds ?? []).Select(id => new TagId(id)).Distinct().ToList();
        var missingTags = await tags.FindMissingAsync(tagIds, cancellationToken);
        if (missingTags.Count > 0)
        {
            return Result.Failure<Guid>(Error.NotFound(
                "tag.not_found",
                "One or more of the supplied tags do not exist."));
        }

        var item = ItemFactory.Create(
            request.Type,
            ItemStem.Create(request.Stem),
            request.Difficulty,
            categoryId,
            Points.Create(request.MaximumScore),
            currentUser.UserId!.Value,
            BuildOptions(request.Options),
            BuildRubric(request.Rubric),
            request.SampleAnswer);

        item.ReplaceTags(tagIds);
        items.Add(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(item.Id.Value);
    }

    private static List<ItemOption> BuildOptions(IReadOnlyList<ItemOptionInput>? options)
        => (options ?? [])
            .Select((option, position) =>
                ItemOption.Create(option.Text, option.IsCorrect, position, option.Feedback))
            .ToList();

    private static EssayRubric? BuildRubric(EssayRubricInput? rubric)
        => rubric is null
            ? null
            : EssayRubric.Create(rubric.Guidance, rubric.MinimumWords, rubric.MaximumWords);
}
