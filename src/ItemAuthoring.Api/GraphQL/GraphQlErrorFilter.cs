using HotChocolate;
using HotChocolate.Execution;
using ItemAuthoring.Api.Common;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Common;
using Error = ItemAuthoring.Application.Common.Error;

namespace ItemAuthoring.Api.GraphQL;

/// <summary>
/// Normalizes unexpected exceptions and domain rule violations raised inside a resolver.
/// </summary>
/// <remarks>
/// This is the GraphQL counterpart of <see cref="GlobalExceptionHandler"/>, and it makes the same two
/// promises: a violated domain rule is reported with its stable code, and an unexpected failure never
/// leaks its message to the caller.
/// </remarks>
/// <param name="logger">The logger.</param>
/// <param name="environment">The hosting environment.</param>
public sealed class GraphQlErrorFilter(ILogger<GraphQlErrorFilter> logger, IHostEnvironment environment)
    : IErrorFilter
{
    /// <inheritdoc />
    public IError OnError(IError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error.Exception switch
        {
            DomainException domainException => GraphQlResultExtensions.ToError(
                Error.Conflict(domainException.Code, domainException.Message)),
            null => error,
            _ => Obscure(error),
        };
    }

    private IError Obscure(IError error)
    {
        logger.LogError(
            error.Exception,
            "Unhandled exception in GraphQL resolver at path {Path}.",
            error.Path?.ToString());

        return environment.IsDevelopment()
            ? error
            : GraphQlResultExtensions.ToError(Error.Failure(
                "server.unexpected",
                "An unexpected error occurred while executing the operation."));
    }
}
