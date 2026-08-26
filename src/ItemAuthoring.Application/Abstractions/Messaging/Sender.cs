using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ItemAuthoring.Application.Abstractions.Messaging;

/// <summary>
/// The in-process request dispatcher.
/// </summary>
/// <remarks>
/// <para>
/// A third party mediator library was deliberately not taken as a dependency. The only feature this
/// application needs is "resolve one handler and wrap it in an ordered list of behaviours", which is
/// the sixty lines below; taking a dependency for that would add a licence constraint and an upgrade
/// obligation without removing any real complexity.
/// </para>
/// <para>
/// Reflection is confined to the one-off construction of a closed generic wrapper per request type.
/// The wrapper is cached, so the steady-state dispatch cost is a dictionary lookup and a virtual
/// call — which matters, because request dispatch sits inside every measurement the study reports.
/// </para>
/// </remarks>
/// <param name="serviceProvider">The scope from which handlers and behaviours are resolved.</param>
internal sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    private static readonly ConcurrentDictionary<Type, object> Wrappers = new();

    /// <inheritdoc />
    public Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestWrapper<TResponse>)Wrappers.GetOrAdd(
            request.GetType(),
            static requestType => CreateWrapper<TResponse>(requestType));

        return wrapper.HandleAsync(serviceProvider, request, cancellationToken);
    }

    private static object CreateWrapper<TResponse>(Type requestType)
    {
        var wrapperType = typeof(RequestWrapper<,>).MakeGenericType(requestType, typeof(TResponse));
        return Activator.CreateInstance(wrapperType)
            ?? throw new InvalidOperationException(
                $"The request wrapper for '{requestType}' could not be created.");
    }

    private abstract class RequestWrapper<TResponse>
    {
        public abstract Task<TResponse> HandleAsync(
            IServiceProvider serviceProvider,
            IRequest<TResponse> request,
            CancellationToken cancellationToken);
    }

    private sealed class RequestWrapper<TRequest, TResponse> : RequestWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> HandleAsync(
            IServiceProvider serviceProvider,
            IRequest<TResponse> request,
            CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;
            var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

            RequestHandlerDelegate<TResponse> pipeline =
                () => handler.HandleAsync(typedRequest, cancellationToken);

            var behaviors = serviceProvider
                .GetServices<IPipelineBehavior<TRequest, TResponse>>()
                .Reverse();

            foreach (var behavior in behaviors)
            {
                var next = pipeline;
                pipeline = () => behavior.HandleAsync(typedRequest, next, cancellationToken);
            }

            return pipeline();
        }
    }
}
