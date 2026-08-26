using GreenDonut;
using HotChocolate;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Exams.Dtos;
using ItemAuthoring.Application.Identity.Dtos;
using ItemAuthoring.Application.Items.Dtos;

namespace ItemAuthoring.Api.GraphQL.DataLoaders;

/// <summary>
/// Batches category lookups made while resolving a list of items.
/// </summary>
/// <remarks>
/// Without a data loader, a query that selects the category of fifty items issues fifty statements —
/// the N+1 problem that is the standard objection to GraphQL. Every collection navigation exposed by
/// this schema is therefore resolved through a loader, and the effect is visible in the
/// <c>databaseCommands</c> column of the benchmark output.
/// </remarks>
/// <param name="readStore">The read side of the taxonomy.</param>
/// <param name="batchScheduler">The scheduler supplied by the execution engine.</param>
/// <param name="options">The data loader options supplied by the execution engine.</param>
public sealed class CategoryByIdDataLoader(
    ITaxonomyReadStore readStore,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<Guid, CategoryDto>(batchScheduler, options)
{
    /// <inheritdoc />
    protected override async Task<IReadOnlyDictionary<Guid, CategoryDto>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
        => await readStore.GetCategoriesAsync(keys, cancellationToken);
}

/// <summary>Batches tag lookups made while resolving a list of items.</summary>
/// <param name="readStore">The read side of the taxonomy.</param>
/// <param name="batchScheduler">The scheduler supplied by the execution engine.</param>
/// <param name="options">The data loader options supplied by the execution engine.</param>
public sealed class TagsByItemDataLoader(
    ITaxonomyReadStore readStore,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<Guid, IReadOnlyList<ItemTagDto>>(batchScheduler, options)
{
    /// <inheritdoc />
    protected override async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemTagDto>>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
        => await readStore.GetTagsByItemAsync(keys, cancellationToken);
}

/// <summary>Batches answer option lookups made while resolving a list of items.</summary>
/// <param name="readStore">The read side of the item bank.</param>
/// <param name="batchScheduler">The scheduler supplied by the execution engine.</param>
/// <param name="options">The data loader options supplied by the execution engine.</param>
public sealed class OptionsByItemDataLoader(
    IItemReadStore readStore,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<Guid, IReadOnlyList<ItemOptionDto>>(batchScheduler, options)
{
    /// <inheritdoc />
    protected override async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemOptionDto>>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
        => await readStore.GetOptionsAsync(keys, cancellationToken);
}

/// <summary>Batches section lookups made while resolving a list of exams.</summary>
/// <param name="readStore">The read side of the exam builder.</param>
/// <param name="batchScheduler">The scheduler supplied by the execution engine.</param>
/// <param name="options">The data loader options supplied by the execution engine.</param>
public sealed class SectionsByExamDataLoader(
    IExamReadStore readStore,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<Guid, IReadOnlyList<ExamSectionDto>>(batchScheduler, options)
{
    /// <inheritdoc />
    protected override async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ExamSectionDto>>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
        => await readStore.GetSectionsAsync(keys, cancellationToken);
}

/// <summary>Batches role lookups made while resolving a list of users.</summary>
/// <param name="readStore">The read side of the user directory.</param>
/// <param name="batchScheduler">The scheduler supplied by the execution engine.</param>
/// <param name="options">The data loader options supplied by the execution engine.</param>
public sealed class RolesByUserDataLoader(
    IIdentityReadStore readStore,
    IBatchScheduler batchScheduler,
    DataLoaderOptions options)
    : BatchDataLoader<Guid, IReadOnlyList<RoleDto>>(batchScheduler, options)
{
    /// <inheritdoc />
    protected override async Task<IReadOnlyDictionary<Guid, IReadOnlyList<RoleDto>>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
        => await readStore.GetRolesByUserAsync(keys, cancellationToken);
}
