using Asp.Versioning;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Identity.Commands;
using ItemAuthoring.Application.Identity.Dtos;
using ItemAuthoring.Application.Identity.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ItemAuthoring.Api.Rest.Controllers;

/// <summary>
/// Sign-in, token refresh and sign-out.
/// </summary>
/// <param name="sender">The request dispatcher.</param>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[EnableRateLimiting(RateLimitingPolicies.Authentication)]
public sealed class AuthController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Exchanges credentials for a token pair.</summary>
    /// <param name="command">The credentials.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The issued tokens and the profile of the signed-in user.</returns>
    [HttpPost("login", Name = nameof(Login))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(command, cancellationToken));

    /// <summary>Exchanges a refresh token for a new token pair.</summary>
    /// <param name="command">The refresh token.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The issued tokens and the profile of the signed-in user.</returns>
    [HttpPost("refresh", Name = nameof(Refresh))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(command, cancellationToken));

    /// <summary>Revokes a refresh token, ending the session it belongs to.</summary>
    /// <param name="command">The refresh token.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>No content, whether or not the token was still valid.</returns>
    [HttpPost("logout", Name = nameof(Logout))]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutCommand command,
        CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(command, cancellationToken));

    /// <summary>Reads the profile and permissions of the caller.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The profile of the caller.</returns>
    [HttpGet("me", Name = nameof(GetCurrentUser))]
    [DisableRateLimiting]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
        => Respond(await Sender.SendAsync(new GetCurrentUserQuery(), cancellationToken));
}
