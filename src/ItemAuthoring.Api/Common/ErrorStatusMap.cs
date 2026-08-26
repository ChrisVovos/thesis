using ItemAuthoring.Application.Common;

namespace ItemAuthoring.Api.Common;
/// <summary>
/// The single mapping from an application <see cref="ErrorType"/> to a transport specific status.
/// </summary>
/// <remarks>
/// Both surfaces read this table. Adding a new error classification therefore updates REST status
/// codes and GraphQL error extensions together, which is the mechanism that keeps the two surfaces
/// behaviourally identical for the purposes of the comparison.
/// </remarks>
public static class ErrorStatusMap
{
    /// <summary>Maps an error classification to an HTTP status code.</summary>
    /// <param name="errorType">The classification to map.</param>
    /// <returns>The HTTP status code.</returns>
    public static int ToStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>Maps an error classification to a short, human readable title.</summary>
    /// <param name="errorType">The classification to map.</param>
    /// <returns>The title used in the problem document.</returns>
    public static string ToTitle(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "One or more validation errors occurred.",
        ErrorType.NotFound => "The requested resource was not found.",
        ErrorType.Conflict => "The request conflicts with the current state of the resource.",
        ErrorType.Unauthorized => "Authentication is required.",
        ErrorType.Forbidden => "The caller is not permitted to perform this operation.",
        _ => "An unexpected error occurred.",
    };

    /// <summary>Maps an error classification to the GraphQL error extension classification.</summary>
    /// <param name="errorType">The classification to map.</param>
    /// <returns>The value published as the <c>classification</c> error extension.</returns>
    public static string ToGraphQlClassification(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation => "BAD_USER_INPUT",
        ErrorType.NotFound => "NOT_FOUND",
        ErrorType.Conflict => "CONFLICT",
        ErrorType.Unauthorized => "UNAUTHENTICATED",
        ErrorType.Forbidden => "FORBIDDEN",
        _ => "INTERNAL_SERVER_ERROR",
    };
}
