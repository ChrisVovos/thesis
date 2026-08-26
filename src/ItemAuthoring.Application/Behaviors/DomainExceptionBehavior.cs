using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Common;
using ItemAuthoring.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ItemAuthoring.Application.Behaviors;

/// <summary>
/// Translates a violated domain invariant into a failed result.
/// </summary>
/// <remarks>
/// Aggregates protect their invariants by throwing, because a method that cannot honour its contract
/// must not return normally. Callers, however, want an outcome rather than an exception, and both API
/// surfaces need the same stable code. This behaviour is the single point where the two views meet,
/// which is why neither the REST nor the GraphQL layer contains a <c>catch (DomainException)</c>.
/// </remarks>
/// <typeparam name="TRequest">The request type being executed.</typeparam>
/// <typeparam name="TResponse">The response type of the pipeline.</typeparam>
/// <param name="logger">The logger for the request type.</param>
internal sealed class DomainExceptionBehavior<TRequest, TResponse>(
    ILogger<DomainExceptionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (DomainException exception) when (ResultFactory.IsResultType(typeof(TResponse)))
        {
            logger.LogInformation(
                "Request {RequestName} was rejected by the domain rule {RuleCode}: {RuleMessage}",
                typeof(TRequest).Name,
                exception.Code,
                exception.Message);

            return ResultFactory.Failure<TResponse>(
                Error.Conflict(exception.Code, exception.Message));
        }
    }
}
