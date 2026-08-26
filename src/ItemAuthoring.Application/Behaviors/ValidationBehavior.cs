using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Common;

namespace ItemAuthoring.Application.Behaviors;

/// <summary>
/// Rejects a request whose input violates its FluentValidation rules before any handler runs.
/// </summary>
/// <remarks>
/// Validation lives here rather than in a controller filter or a GraphQL input validator so that the
/// two API surfaces cannot drift apart: a rule added once is enforced for both, and the resulting
/// error carries the same code and the same per-field details on both.
/// </remarks>
/// <typeparam name="TRequest">The request type being validated.</typeparam>
/// <typeparam name="TResponse">The response type of the pipeline.</typeparam>
/// <param name="validators">Every validator registered for the request type.</param>
internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var applicable = validators.ToList();
        if (applicable.Count == 0)
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            applicable.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        var details = failures
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        return ResultFactory.Failure<TResponse>(Error.Validation(
            "validation.failed",
            "One or more input values are invalid.",
            details));
    }
}
