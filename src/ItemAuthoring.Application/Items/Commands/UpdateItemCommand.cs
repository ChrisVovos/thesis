using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Items.Commands;

/// <summary>Replaces the content of a draft item.</summary>
/// <param name="ItemId">The item to update.</param>
/// <param name="Stem">The new prompt.</param>
/// <param name="Difficulty">The new cognitive demand.</param>
/// <param name="CategoryId">The new category.</param>
/// <param name="MaximumScore">The new maximum score.</param>
/// <param name="Options">The new answer options, for every shape except essay.</param>
/// <param name="Rubric">The new grading guidance, for essay items.</param>
/// <param name="SampleAnswer">The new exemplar answer, for essay items.</param>
/// <param name="TagIds">The tags the item should carry afterwards.</param>
[RequiresPermission(Permissions.ItemsUpdate)]
public sealed record UpdateItemCommand(
    Guid ItemId,
    string Stem,
    DifficultyLevel Difficulty,
    Guid CategoryId,
    decimal MaximumScore,
    IReadOnlyList<ItemOptionInput>? Options = null,
    EssayRubricInput? Rubric = null,
    string? SampleAnswer = null,
    IReadOnlyList<Guid>? TagIds = null) : ICommand<Result>;

/// <summary>Validates <see cref="UpdateItemCommand"/>.</summary>
public sealed class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateItemCommandValidator"/> class.</summary>
    public UpdateItemCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty();
        RuleFor(command => command.Difficulty).IsInEnum();
        RuleFor(command => command.Stem).NotEmpty().MaximumLength(ItemStem.MaxLength);
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.MaximumScore).GreaterThan(0m).LessThanOrEqualTo(Points.MaxValue);
        RuleFor(command => command.SampleAnswer)
            .MaximumLength(EssayItem.MaxSampleAnswerLength)
            .When(command => command.SampleAnswer is not null);
        RuleForEach(command => command.Options)
            .ChildRules(option => option.RuleFor(candidate => candidate.Text)
                .NotEmpty()
                .MaximumLength(OptionText.MaxLength))
            .When(command => command.Options is not null);
    }
}

/// <summary>Handles <see cref="UpdateItemCommand"/>.</summary>
/// <param name="items">The item repository.</param>
/// <param name="categories">The category repository.</param>
/// <param name="tags">The tag repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class UpdateItemCommandHandler(
    IItemRepository items,
    ICategoryRepository categories,
    ITagRepository tags,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateItemCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        var item = await items.GetAsync(new ItemId(request.ItemId), cancellationToken);
        if (item is null)
        {
            return Result.Failure(Error.NotFound("item.not_found", "The item does not exist."));
        }

        if (!ItemOwnershipPolicy.CanEdit(item, currentUser))
        {
            return Result.Failure(Error.Forbidden(
                "item.not_owner",
                "Only the author of an item, or an administrator, may edit it."));
        }

        var categoryId = new CategoryId(request.CategoryId);
        if (!await categories.IsActiveAsync(categoryId, cancellationToken))
        {
            return Result.Failure(Error.NotFound(
                "category.not_found",
                "The category does not exist or no longer accepts new items."));
        }

        var tagIds = (request.TagIds ?? []).Select(id => new TagId(id)).Distinct().ToList();
        if ((await tags.FindMissingAsync(tagIds, cancellationToken)).Count > 0)
        {
            return Result.Failure(Error.NotFound(
                "tag.not_found",
                "One or more of the supplied tags do not exist."));
        }

        item.UpdateDetails(
            ItemStem.Create(request.Stem),
            request.Difficulty,
            categoryId,
            Points.Create(request.MaximumScore));
        item.ReplaceTags(tagIds);
        ApplyShapeSpecificContent(item, request);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static void ApplyShapeSpecificContent(Item item, UpdateItemCommand request)
    {
        switch (item)
        {
            case ChoiceItem choiceItem:
                choiceItem.ReplaceOptions((request.Options ?? [])
                    .Select((option, position) =>
                        ItemOption.Create(option.Text, option.IsCorrect, position, option.Feedback)));
                break;

            case EssayItem essayItem when request.Rubric is { } rubric:
                essayItem.UpdateRubric(
                    EssayRubric.Create(rubric.Guidance, rubric.MinimumWords, rubric.MaximumWords),
                    request.SampleAnswer);
                break;

            default:
                break;
        }
    }
}
