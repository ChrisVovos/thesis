using System.Collections.Concurrent;
using System.Reflection;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Common;

namespace ItemAuthoring.Application.Behaviors;

/// <summary>
/// Enforces the access rules declared on a request.
/// </summary>
/// <remarks>
/// Requests are authenticated by default and must opt out explicitly with
/// <see cref="AllowAnonymousRequestAttribute"/>. A request that declares no permission and no opt-out
/// is therefore reachable by any signed-in user, and one that forgets to declare anything can never
/// accidentally become anonymous — the failure mode is a denial, not a leak.
/// </remarks>
/// <typeparam name="TRequest">The request type being authorized.</typeparam>
/// <typeparam name="TResponse">The response type of the pipeline.</typeparam>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class AuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, RequestAccessRule> Rules = new();

    /// <inheritdoc />
    public Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var rule = Rules.GetOrAdd(typeof(TRequest), static type => RequestAccessRule.For(type));

        if (rule.AllowAnonymous)
        {
            return next();
        }

        if (!currentUser.IsAuthenticated)
        {
            return Task.FromResult(ResultFactory.Failure<TResponse>(Error.Unauthorized(
                "auth.required",
                "Authentication is required to perform this operation.")));
        }

        var missing = rule.RequiredPermissions
            .Where(permission => !currentUser.HasPermission(permission))
            .ToList();

        if (missing.Count > 0)
        {
            return Task.FromResult(ResultFactory.Failure<TResponse>(Error.Forbidden(
                "auth.forbidden",
                $"The operation requires the '{string.Join("', '", missing)}' permission.")));
        }

        return next();
    }

    private sealed record RequestAccessRule(bool AllowAnonymous, IReadOnlyList<string> RequiredPermissions)
    {
        public static RequestAccessRule For(Type requestType)
        {
            var allowAnonymous = requestType
                .GetCustomAttribute<AllowAnonymousRequestAttribute>(inherit: false) is not null;

            var permissions = requestType
                .GetCustomAttributes<RequiresPermissionAttribute>(inherit: false)
                .Select(attribute => attribute.Permission)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return new RequestAccessRule(allowAnonymous, permissions);
        }
    }
}
