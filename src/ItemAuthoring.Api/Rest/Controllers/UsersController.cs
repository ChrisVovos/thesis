using Asp.Versioning;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Application.Identity.Commands;
using ItemAuthoring.Application.Identity.Dtos;
using ItemAuthoring.Application.Identity.Queries;
using Microsoft.AspNetCore.Mvc;

namespace ItemAuthoring.Api.Rest.Controllers;

/// <summary>
/// User administration.
/// </summary>
/// <param name="sender">The request dispatcher.</param>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Searches, sorts and pages the user directory.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <param name="page">The one based page index.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="search">A free-text search term.</param>
    /// <param name="sortDescending">Whether the sort is descending.</param>
    /// <param name="isActive">The activation state to filter on.</param>
    /// <param name="roleId">The role to restrict the search to.</param>
    /// <returns>One page of users together with paging metadata.</returns>
    [HttpGet(Name = nameof(SearchUsers))]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchUsers(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PagedQuery.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] bool sortDescending = false,
        [FromQuery] bool? isActive = null,
        [FromQuery] Guid? roleId = null)
    {
        var criteria = new UserSearchCriteria
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            SortDescending = sortDescending,
            IsActive = isActive,
            RoleId = roleId,
        };
        return Respond(await Sender.SendAsync(new SearchUsersQuery(criteria), cancellationToken));
    }

    /// <summary>Reads a single user.</summary>
    /// <param name="id">The identity of the user.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The user.</returns>
    [HttpGet("{id:guid}", Name = nameof(GetUser))]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new GetUserByIdQuery(id), cancellationToken));

    /// <summary>Creates a user account.</summary>
    /// <param name="command">The account to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new user.</returns>
    [HttpPost(Name = nameof(CreateUser))]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(command, cancellationToken);
        return RespondCreated(result, nameof(GetUser), new { id = result.IsSuccess ? result.Value : Guid.Empty });
    }

    /// <summary>Replaces the profile and role assignment of a user.</summary>
    /// <param name="id">The identity of the user.</param>
    /// <param name="command">The new profile.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id:guid}", Name = nameof(UpdateUser))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Respond(await Sender.SendAsync(command with { UserId = id }, cancellationToken));
    }

    /// <summary>Activates or deactivates a user account.</summary>
    /// <param name="id">The identity of the user.</param>
    /// <param name="isActive">Whether the user may sign in.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id:guid}/active", Name = nameof(SetUserActive))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetUserActive(
        Guid id,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new SetUserActiveCommand(id, isActive), cancellationToken));

    /// <summary>Replaces a user's password and revokes their outstanding sessions.</summary>
    /// <param name="id">The identity of the user.</param>
    /// <param name="command">The new password.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id:guid}/password", Name = nameof(ResetUserPassword))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetUserPassword(
        Guid id,
        [FromBody] ResetUserPasswordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Respond(await Sender.SendAsync(command with { UserId = id }, cancellationToken));
    }
}
