using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Exams.Dtos;
using ItemAuthoring.Application.Exams.Queries;
using ItemAuthoring.Application.Identity.Dtos;
using ItemAuthoring.Application.Identity.Queries;
using ItemAuthoring.Application.Items.Dtos;
using ItemAuthoring.Application.Items.Queries;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Api.GraphQL;

/// <summary>
/// The root query type.
/// </summary>
/// <remarks>
/// <para>
/// Two styles of read field coexist deliberately, and the study compares them.
/// </para>
/// <para>
/// The <c>items</c>, <c>exams</c> and <c>users</c> fields expose a composable
/// <see cref="IQueryable{T}"/>: filtering, sorting and Relay-style cursor paging arrive as GraphQL
/// arguments and Hot Chocolate rewrites them into the same expression tree Entity Framework Core
/// turns into SQL, so the database does the work.
/// </para>
/// <para>
/// The <c>searchItems</c>, <c>searchExams</c> and <c>searchUsers</c> fields offer the offset paging
/// the REST surface uses, dispatching the same query object the REST controllers dispatch. That is
/// what makes the like-for-like comparison possible: identical handler, identical SQL, identical
/// authorization — the only difference is the transport.
/// </para>
/// </remarks>
public sealed class Query
{
    /// <summary>Exposes the item bank as a filterable, sortable, pageable collection.</summary>
    /// <param name="readStore">The read side of the item bank.</param>
    /// <param name="guard">The permission guard.</param>
    /// <returns>The composable item query.</returns>
    [UsePaging(MaxPageSize = 100, IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ItemSummaryDto> GetItems(
        [Service] IItemReadStore readStore,
        [Service] IPermissionGuard guard)
    {
        guard.Require(Permissions.ItemsRead).UnwrapOrThrow();
        return readStore.QuerySummaries();
    }

    /// <summary>Reads a single item through the same query handler the REST surface uses.</summary>
    /// <param name="id">The identity of the item.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The full projection of the item.</returns>
    public async Task<ItemDetailDto> GetItemById(
        Guid id,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new GetItemByIdQuery(id), cancellationToken)).UnwrapOrThrow();

    /// <summary>Reads the published versions of an item, newest first.</summary>
    /// <param name="itemId">The identity of the item.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The published versions.</returns>
    public async Task<IReadOnlyList<ItemVersionDto>> GetItemVersions(
        Guid itemId,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new GetItemVersionsQuery(itemId), cancellationToken)).UnwrapOrThrow();

    /// <summary>Reads the complete category taxonomy.</summary>
    /// <param name="readStore">The read side of the taxonomy.</param>
    /// <param name="guard">The permission guard.</param>
    /// <returns>The composable category query.</returns>
    [UseFiltering]
    [UseSorting]
    public IQueryable<CategoryDto> GetCategories(
        [Service] ITaxonomyReadStore readStore,
        [Service] IPermissionGuard guard)
    {
        guard.Require(Permissions.ItemsRead).UnwrapOrThrow();
        return readStore.QueryCategories();
    }

    /// <summary>Reads every tag.</summary>
    /// <param name="readStore">The read side of the taxonomy.</param>
    /// <param name="guard">The permission guard.</param>
    /// <returns>The composable tag query.</returns>
    [UseFiltering]
    [UseSorting]
    public IQueryable<TagDto> GetTags(
        [Service] ITaxonomyReadStore readStore,
        [Service] IPermissionGuard guard)
    {
        guard.Require(Permissions.ItemsRead).UnwrapOrThrow();
        return readStore.QueryTags();
    }

    /// <summary>Exposes the exam list as a filterable, sortable, pageable collection.</summary>
    /// <param name="readStore">The read side of the exam builder.</param>
    /// <param name="guard">The permission guard.</param>
    /// <returns>The composable exam query.</returns>
    [UsePaging(MaxPageSize = 100, IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ExamSummaryDto> GetExams(
        [Service] IExamReadStore readStore,
        [Service] IPermissionGuard guard)
    {
        guard.Require(Permissions.ExamsRead).UnwrapOrThrow();
        return readStore.QuerySummaries();
    }

    /// <summary>Reads a single exam together with its full composition.</summary>
    /// <param name="id">The identity of the exam.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The full projection of the exam.</returns>
    public async Task<ExamDetailDto> GetExamById(
        Guid id,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new GetExamByIdQuery(id), cancellationToken)).UnwrapOrThrow();

    /// <summary>Exposes the user directory as a filterable, sortable, pageable collection.</summary>
    /// <param name="readStore">The read side of the user directory.</param>
    /// <param name="guard">The permission guard.</param>
    /// <returns>The composable user query.</returns>
    [UsePaging(MaxPageSize = 100, IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<UserDto> GetUsers(
        [Service] IIdentityReadStore readStore,
        [Service] IPermissionGuard guard)
    {
        guard.Require(Permissions.UsersRead).UnwrapOrThrow();
        return readStore.QueryUsers();
    }

    /// <summary>Reads every role together with its permissions.</summary>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>Every role.</returns>
    public async Task<IReadOnlyList<RoleDto>> GetRoles(
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new ListRolesQuery(), cancellationToken)).UnwrapOrThrow();

    /// <summary>Reads the permission catalogue.</summary>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The permission catalogue.</returns>
    public async Task<IReadOnlyList<PermissionDto>> GetPermissions(
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new ListPermissionsQuery(), cancellationToken)).UnwrapOrThrow();

    /// <summary>Reads the profile and permissions of the caller.</summary>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The profile of the caller.</returns>
    public async Task<CurrentUserDto> GetMe(
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new GetCurrentUserQuery(), cancellationToken)).UnwrapOrThrow();

    /// <summary>Runs the same paged item search the REST surface exposes, for a like-for-like comparison.</summary>
    /// <param name="criteria">The search, filter, sort and paging criteria.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>One page of item summaries together with paging metadata.</returns>
    public async Task<PagedResult<ItemSummaryDto>> SearchItems(
        ItemSearchCriteria criteria,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new SearchItemsQuery(criteria), cancellationToken)).UnwrapOrThrow();

    /// <summary>Runs the same paged exam search the REST surface exposes.</summary>
    /// <param name="criteria">The search, filter, sort and paging criteria.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>One page of exam summaries together with paging metadata.</returns>
    public async Task<PagedResult<ExamSummaryDto>> SearchExams(
        ExamSearchCriteria criteria,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new SearchExamsQuery(criteria), cancellationToken)).UnwrapOrThrow();

    /// <summary>Runs the same paged user search the REST surface exposes.</summary>
    /// <param name="criteria">The search, sort and paging criteria.</param>
    /// <param name="sender">The request dispatcher.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>One page of users together with paging metadata.</returns>
    public async Task<PagedResult<UserDto>> SearchUsers(
        UserSearchCriteria criteria,
        [Service] ISender sender,
        CancellationToken cancellationToken)
        => (await sender.SendAsync(new SearchUsersQuery(criteria), cancellationToken)).UnwrapOrThrow();
}
