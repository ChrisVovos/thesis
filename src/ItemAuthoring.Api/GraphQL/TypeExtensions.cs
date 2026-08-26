using HotChocolate;
using HotChocolate.Types;
using ItemAuthoring.Api.GraphQL.DataLoaders;
using ItemAuthoring.Application.Exams.Dtos;
using ItemAuthoring.Application.Identity.Dtos;
using ItemAuthoring.Application.Items.Dtos;

namespace ItemAuthoring.Api.GraphQL;

/// <summary>
/// Adds batched navigation fields to the item summary type.
/// </summary>
/// <remarks>
/// These fields are the reason GraphQL can answer "give me the items and, for each, its category and
/// its options" in a bounded number of statements. Every one of them resolves through a data loader,
/// which is what the <c>databaseCommands</c> figure in the benchmark output demonstrates.
/// </remarks>
[ExtendObjectType<ItemSummaryDto>]
public sealed class ItemSummaryTypeExtensions
{
    /// <summary>Resolves the category the item is filed under.</summary>
    /// <param name="item">The item being resolved.</param>
    /// <param name="loader">The batching category loader.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The category.</returns>
    public Task<CategoryDto?> GetCategoryAsync(
        [Parent] ItemSummaryDto item,
        CategoryByIdDataLoader loader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        return loader.LoadAsync(item.CategoryId, cancellationToken)!;
    }

    /// <summary>Resolves the answer options of the item.</summary>
    /// <param name="item">The item being resolved.</param>
    /// <param name="loader">The batching option loader.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The answer options, empty for essay items.</returns>
    public async Task<IReadOnlyList<ItemOptionDto>> GetOptionsAsync(
        [Parent] ItemSummaryDto item,
        OptionsByItemDataLoader loader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        return await loader.LoadAsync(item.Id, cancellationToken) ?? [];
    }
}

/// <summary>Adds batched navigation fields to the exam summary type.</summary>
[ExtendObjectType<ExamSummaryDto>]
public sealed class ExamSummaryTypeExtensions
{
    /// <summary>Resolves the sections of the exam.</summary>
    /// <param name="exam">The exam being resolved.</param>
    /// <param name="loader">The batching section loader.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The sections in display order.</returns>
    public async Task<IReadOnlyList<ExamSectionDto>> GetSectionsAsync(
        [Parent] ExamSummaryDto exam,
        SectionsByExamDataLoader loader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exam);
        return await loader.LoadAsync(exam.Id, cancellationToken) ?? [];
    }
}

/// <summary>Adds a batched navigation field to the exam item placement type.</summary>
[ExtendObjectType<ExamItemDto>]
public sealed class ExamItemTypeExtensions
{
    /// <summary>Resolves the category of the referenced bank item.</summary>
    /// <param name="placement">The placement being resolved.</param>
    /// <param name="loader">The batching category loader.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The category, or <see langword="null"/> when the item was not hydrated.</returns>
    public Task<CategoryDto?> GetCategoryAsync(
        [Parent] ExamItemDto placement,
        CategoryByIdDataLoader loader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(placement);
        return placement.Item is null
            ? Task.FromResult<CategoryDto?>(null)
            : loader.LoadAsync(placement.Item.CategoryId, cancellationToken)!;
    }
}

/// <summary>Adds a batched navigation field to the user type.</summary>
[ExtendObjectType<UserDto>]
public sealed class UserTypeExtensions
{
    /// <summary>Resolves the roles assigned to the user.</summary>
    /// <param name="user">The user being resolved.</param>
    /// <param name="loader">The batching role loader.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The assigned roles.</returns>
    public async Task<IReadOnlyList<RoleDto>> GetAssignedRolesAsync(
        [Parent] UserDto user,
        RolesByUserDataLoader loader,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return await loader.LoadAsync(user.Id, cancellationToken) ?? [];
    }
}
