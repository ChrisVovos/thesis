using Asp.Versioning;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Identity.Commands;
using ItemAuthoring.Application.Identity.Dtos;
using ItemAuthoring.Application.Identity.Queries;
using Microsoft.AspNetCore.Mvc;

namespace ItemAuthoring.Api.Rest.Controllers;

/// <summary>
/// Role administration.
/// </summary>
/// <param name="sender">The request dispatcher.</param>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
public sealed class RolesController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Reads every role together with its permissions.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>Every role.</returns>
    [HttpGet(Name = nameof(ListRoles))]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRoles(CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new ListRolesQuery(), cancellationToken));

    /// <summary>Creates a role.</summary>
    /// <param name="command">The role to create.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The identity of the new role.</returns>
    [HttpPost(Name = nameof(CreateRole))]
    [ProducesResponseType(typeof(CreatedResourceResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Sender.SendAsync(command, cancellationToken);
        return RespondCreated(result, nameof(ListRoles), new { });
    }

    /// <summary>Replaces the description and permission set of a role.</summary>
    /// <param name="id">The identity of the role.</param>
    /// <param name="command">The new definition.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpPut("{id:guid}", Name = nameof(UpdateRole))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Respond(await Sender.SendAsync(command with { RoleId = id }, cancellationToken));
    }

    /// <summary>Deletes a role that no user holds.</summary>
    /// <param name="id">The identity of the role.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}", Name = nameof(DeleteRole))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new DeleteRoleCommand(id), cancellationToken));
}

/// <summary>
/// The permission catalogue.
/// </summary>
/// <param name="sender">The request dispatcher.</param>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/permissions")]
public sealed class PermissionsController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Reads every permission known to the application.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The permission catalogue.</returns>
    [HttpGet(Name = nameof(ListPermissions))]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPermissions(CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new ListPermissionsQuery(), cancellationToken));
}
