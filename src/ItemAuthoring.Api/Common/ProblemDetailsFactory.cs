using ItemAuthoring.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Error = ItemAuthoring.Application.Common.Error;

namespace ItemAuthoring.Api.Common;

/// <summary>
/// Builds RFC 9457 problem documents from application errors.
/// </summary>
/// <remarks>
/// The name is prefixed to avoid colliding with the <c>ProblemDetailsFactory</c> property that
/// <see cref="ControllerBase"/> already exposes, which would otherwise shadow this type inside every
/// controller.
/// </remarks>
public static class ApiProblemDetailsFactory
{
    /// <summary>The base URI under which the error type documentation is published.</summary>
    public const string TypeBaseUri = "https://itemauthoring.example/errors/";

    /// <summary>Creates a problem document describing the supplied error.</summary>
    /// <param name="error">The application error.</param>
    /// <param name="instance">The request path the error occurred on.</param>
    /// <param name="correlationId">The correlation identifier of the request.</param>
    /// <returns>The problem document.</returns>
    public static ProblemDetails Create(Error error, string? instance, string? correlationId)
    {
        ArgumentNullException.ThrowIfNull(error);

        var problem = new ProblemDetails
        {
            Type = TypeBaseUri + error.Code,
            Title = ErrorStatusMap.ToTitle(error.Type),
            Detail = error.Message,
            Status = ErrorStatusMap.ToStatusCode(error.Type),
            Instance = instance,
        };

        problem.Extensions["code"] = error.Code;

        if (correlationId is not null)
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        if (error.Details is { Count: > 0 })
        {
            problem.Extensions["errors"] = error.Details;
        }

        return problem;
    }
}
