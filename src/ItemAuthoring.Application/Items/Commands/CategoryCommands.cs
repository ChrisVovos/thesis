using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Identity;
using ItemAuthoring.Domain.Items;

namespace ItemAuthoring.Application.Items.Commands;

/// <summary>Creates a category in the item bank taxonomy.</summary>
/// <param name="Name">The display name.</param>
/// <param name="Description">An optional description.</param>
/// <param name="ParentCategoryId">The parent category, or <see langword="null"/> for a root.</param>
[RequiresPermission(Permissions.TaxonomyManage)]
public sealed record CreateCategoryCommand(string Name, string? Description, Guid? ParentCategoryId)
    : ICommand<Result<Guid>>;

/// <summary>Renames or re-parents a category.</summary>
/// <param name="CategoryId">The category to update.</param>
/// <param name="Name">The new display name.</param>
/// <param name="Description">The new description.</param>
/// <param name="ParentCategoryId">The new parent category.</param>
/// <param name="IsActive">Whether new items may be filed under the category.</param>
[RequiresPermission(Permissions.TaxonomyManage)]
public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    bool IsActive) : ICommand<Result>;

/// <summary>Deletes a category that holds no items.</summary>
/// <param name="CategoryId">The category to delete.</param>
[RequiresPermission(Permissions.TaxonomyManage)]
public sealed record DeleteCategoryCommand(Guid CategoryId) : ICommand<Result>;

/// <summary>Validates <see cref="CreateCategoryCommand"/>.</summary>
public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateCategoryCommandValidator"/> class.</summary>
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(CategoryName.MaxLength);
        RuleFor(command => command.Description).MaximumLength(Category.MaxDescriptionLength);
    }
}

/// <summary>Validates <see cref="UpdateCategoryCommand"/>.</summary>
public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateCategoryCommandValidator"/> class.</summary>
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.CategoryId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(CategoryName.MaxLength);
        RuleFor(command => command.Description).MaximumLength(Category.MaxDescriptionLength);
    }
}

/// <summary>Handles <see cref="CreateCategoryCommand"/>.</summary>
/// <param name="categories">The category repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class CreateCategoryCommandHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> HandleAsync(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var parentId = request.ParentCategoryId is { } parent ? new CategoryId(parent) : (CategoryId?)null;
        if (parentId is { } id && await categories.GetAsync(id, cancellationToken) is null)
        {
            return Result.Failure<Guid>(Error.NotFound(
                "category.parent_not_found",
                "The parent category does not exist."));
        }

        if (await categories.NameExistsAsync(request.Name.Trim(), parentId, null, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict(
                "category.name_taken",
                "A sibling category already uses that name."));
        }

        var category = Category.Create(
            CategoryName.Create(request.Name),
            request.Description,
            parentId);
        categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(category.Id.Value);
    }
}

/// <summary>Handles <see cref="UpdateCategoryCommand"/>.</summary>
/// <param name="categories">The category repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCategoryCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId);
        var category = await categories.GetAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(Error.NotFound("category.not_found", "The category does not exist."));
        }

        var parentId = request.ParentCategoryId is { } parent ? new CategoryId(parent) : (CategoryId?)null;
        if (await categories.NameExistsAsync(
                request.Name.Trim(), parentId, categoryId, cancellationToken))
        {
            return Result.Failure(Error.Conflict(
                "category.name_taken",
                "A sibling category already uses that name."));
        }

        category.Rename(CategoryName.Create(request.Name));
        category.Describe(request.Description);
        category.MoveTo(parentId);
        if (request.IsActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Handles <see cref="DeleteCategoryCommand"/>.</summary>
/// <param name="categories">The category repository.</param>
/// <param name="unitOfWork">The unit of work.</param>
internal sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCategoryCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> HandleAsync(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId);
        var category = await categories.GetAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure(Error.NotFound("category.not_found", "The category does not exist."));
        }

        if (await categories.HasItemsAsync(categoryId, cancellationToken))
        {
            return Result.Failure(Error.Conflict(
                "category.in_use",
                "The category still holds items and cannot be deleted."));
        }

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
