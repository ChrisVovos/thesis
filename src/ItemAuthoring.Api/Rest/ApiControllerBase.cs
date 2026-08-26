using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ItemAuthoring.Api.Common;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Error = ItemAuthoring.Application.Common.Error;

namespace ItemAuthoring.Api.Rest;

/// <summary>
/// Base class for every REST controller.
/// </summary>
/// <remarks>
/// Controllers are adapters and nothing else: they bind a request, dispatch it, and translate the
/// resulting <see cref="Result"/> into a status code. There is no branch here that a GraphQL resolver
/// does not also have, and no rule that only one of the two enforces.
/// </remarks>
/// <param name="sender">The request dispatcher.</param>
[ApiController]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase(ISender sender) : ControllerBase
{
    /// <summary>Gets the request dispatcher.</summary>
    protected ISender Sender { get; } = sender;

    /// <summary>Translates a value-carrying result into an HTTP response.</summary>
    /// <typeparam name="TValue">The type carried by a successful result.</typeparam>
    /// <param name="result">The outcome of the use case.</param>
    /// <returns>The HTTP response.</returns>
    protected IActionResult Respond<TValue>(Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    /// <summary>Translates a result without a value into an HTTP response.</summary>
    /// <param name="result">The outcome of the use case.</param>
    /// <returns><c>204 No Content</c> on success, otherwise a problem document.</returns>
    protected IActionResult Respond(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    /// <summary>Translates a creation result into a <c>201 Created</c> response.</summary>
    /// <param name="result">The outcome of the use case.</param>
    /// <param name="actionName">The action that reads the created resource.</param>
    /// <param name="routeValues">The route values identifying the created resource.</param>
    /// <returns>The HTTP response.</returns>
    protected IActionResult RespondCreated(Result<Guid> result, string actionName, object routeValues)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess
            ? CreatedAtAction(actionName, routeValues, new CreatedResourceResponse(result.Value))
            : Problem(result.Error);
    }

    /// <summary>Builds an RFC 9457 problem response from an application error.</summary>
    /// <param name="error">The application error.</param>
    /// <returns>The HTTP response.</returns>
    protected IActionResult Problem(Error error)
    {
        var problem = ApiProblemDetailsFactory.Create(
            error,
            HttpContext.Request.Path,
            HttpContext.GetCorrelationId());
        return StatusCode(problem.Status!.Value, problem);
    }

    /// <summary>
    /// Answers a read request with a strong entity tag, returning <c>304 Not Modified</c> when the
    /// caller already holds the current representation.
    /// </summary>
    /// <typeparam name="TValue">The type carried by a successful result.</typeparam>
    /// <param name="result">The outcome of the query.</param>
    /// <param name="versionToken">A value that changes whenever the representation changes.</param>
    /// <param name="maxAge">How long a client may reuse the representation without revalidating.</param>
    /// <returns>The HTTP response.</returns>
    protected IActionResult RespondWithEntityTag<TValue>(
        Result<TValue> result,
        string versionToken,
        TimeSpan maxAge)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsFailure)
        {
            return Problem(result.Error);
        }

        var entityTag = ComputeEntityTag(versionToken);
        Response.Headers.ETag = entityTag;
        Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Private = true,
            MaxAge = maxAge,
        };

        var presented = Request.Headers.IfNoneMatch;
        return presented.Count > 0 && presented.Contains(entityTag)
            ? StatusCode(StatusCodes.Status304NotModified)
            : Ok(result.Value);
    }

    private static string ComputeEntityTag(string versionToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(versionToken));
        return string.Create(
            CultureInfo.InvariantCulture,
            $"\"{Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant()}\"");
    }
}

/// <summary>The body of a <c>201 Created</c> response.</summary>
/// <param name="Id">The identity of the newly created resource.</param>
public sealed record CreatedResourceResponse(Guid Id);
