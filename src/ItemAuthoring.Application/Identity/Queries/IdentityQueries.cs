using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Identity.Dtos;
using ItemAuthoring.Domain.Identity;

namespace ItemAuthoring.Application.Identity.Queries;

/// <summary>Returns the profile and permissions of the caller.</summary>
public sealed record GetCurrentUserQuery : IQuery<Result<CurrentUserDto>>;

/// <summary>Searches, sorts and pages the user directory.</summary>
public sealed record UserSearchCriteria : PagedQuery
{
    /// <summary>Gets the activation state to filter on, when one was supplied.</summary>
    public bool? IsActive { get; init; }

    /// <summary>Gets the role to restrict the search to.</summary>
    public Guid? RoleId { get; init; }
}

/// <summary>Returns one page of the user directory.</summary>
/// <param name="Criteria">The search, sort and paging criteria.</param>
[RequiresPermission(Permissions.UsersRead)]
public sealed record SearchUsersQuery(UserSearchCriteria Criteria)
    : IQuery<Result<PagedResult<UserDto>>>;

/// <summary>Returns a single user.</summary>
/// <param name="UserId">The user to load.</param>
[RequiresPermission(Permissions.UsersRead)]
public sealed record GetUserByIdQuery(Guid UserId) : IQuery<Result<UserDto>>;

/// <summary>Returns every role together with its permissions.</summary>
[RequiresPermission(Permissions.UsersRead)]
public sealed record ListRolesQuery : IQuery<Result<IReadOnlyList<RoleDto>>>;

/// <summary>Returns the permission catalogue.</summary>
[RequiresPermission(Permissions.UsersRead)]
public sealed record ListPermissionsQuery : IQuery<Result<IReadOnlyList<PermissionDto>>>;

/// <summary>Handles <see cref="GetCurrentUserQuery"/>.</summary>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
/// <param name="readStore">The read side of the user directory.</param>
internal sealed class GetCurrentUserQueryHandler(
    ICurrentUser currentUser,
    IIdentityReadStore readStore)
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    /// <inheritdoc />
    public async Task<Result<CurrentUserDto>> HandleAsync(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure<CurrentUserDto>(Error.Unauthorized(
                "auth.required",
                "Authentication is required to perform this operation."));
        }

        var user = await readStore.GetUserAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure<CurrentUserDto>(Error.NotFound(
                "user.not_found",
                "The user does not exist."));
        }

        return Result.Success(new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Roles = [.. currentUser.Roles.Order(StringComparer.Ordinal)],
            Permissions = [.. currentUser.Permissions.Order(StringComparer.Ordinal)],
        });
    }
}

/// <summary>Handles <see cref="SearchUsersQuery"/>.</summary>
/// <param name="readStore">The read side of the user directory.</param>
/// <param name="executor">The asynchronous query executor.</param>
internal sealed class SearchUsersQueryHandler(
    IIdentityReadStore readStore,
    IAsyncQueryExecutor executor)
    : IRequestHandler<SearchUsersQuery, Result<PagedResult<UserDto>>>
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<UserDto>>> HandleAsync(
        SearchUsersQuery request,
        CancellationToken cancellationToken)
    {
        var criteria = request.Criteria;
        var query = readStore.QueryUsers();

        if (criteria.IsActive is { } isActive)
        {
            query = query.Where(user => user.IsActive == isActive);
        }

        if (criteria.RoleId is { } roleId)
        {
            query = query.Where(user => user.Roles.Any(role => role.Id == roleId));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = criteria.Search.Trim();
            query = query.Where(user =>
                user.DisplayName.Contains(term) || user.Email.Contains(term));
        }

        var totalCount = await executor.CountAsync(query, cancellationToken);
        if (totalCount == 0)
        {
            return Result.Success(PagedResult<UserDto>.Empty(criteria.Page, criteria.PageSize));
        }

        var ordered = criteria.SortDescending
            ? query.OrderByDescending(user => user.DisplayName).ThenBy(user => user.Id)
            : query.OrderBy(user => user.DisplayName).ThenBy(user => user.Id);

        var page = await executor.ToListAsync(
            ordered.Skip(criteria.Skip).Take(criteria.PageSize),
            cancellationToken);

        return Result.Success(new PagedResult<UserDto>(page, totalCount, criteria.Page, criteria.PageSize));
    }
}

/// <summary>Handles <see cref="GetUserByIdQuery"/>.</summary>
/// <param name="readStore">The read side of the user directory.</param>
internal sealed class GetUserByIdQueryHandler(IIdentityReadStore readStore)
    : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    /// <inheritdoc />
    public async Task<Result<UserDto>> HandleAsync(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await readStore.GetUserAsync(request.UserId, cancellationToken);
        return user is null
            ? Result.Failure<UserDto>(Error.NotFound("user.not_found", "The user does not exist."))
            : Result.Success(user);
    }
}

/// <summary>Handles <see cref="ListRolesQuery"/>.</summary>
/// <param name="readStore">The read side of the user directory.</param>
/// <param name="executor">The asynchronous query executor.</param>
internal sealed class ListRolesQueryHandler(
    IIdentityReadStore readStore,
    IAsyncQueryExecutor executor)
    : IRequestHandler<ListRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoleDto>>> HandleAsync(
        ListRolesQuery request,
        CancellationToken cancellationToken)
        => Result.Success(await executor.ToListAsync(
            readStore.QueryRoles().OrderBy(role => role.Name),
            cancellationToken));
}

/// <summary>Handles <see cref="ListPermissionsQuery"/>.</summary>
/// <param name="readStore">The read side of the user directory.</param>
internal sealed class ListPermissionsQueryHandler(IIdentityReadStore readStore)
    : IRequestHandler<ListPermissionsQuery, Result<IReadOnlyList<PermissionDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PermissionDto>>> HandleAsync(
        ListPermissionsQuery request,
        CancellationToken cancellationToken)
        => Result.Success(await readStore.GetPermissionsAsync(cancellationToken));
}
