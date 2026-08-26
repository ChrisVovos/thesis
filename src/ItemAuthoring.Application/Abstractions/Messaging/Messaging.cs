namespace ItemAuthoring.Application.Abstractions.Messaging;

/// <summary>
/// A message that is dispatched to exactly one handler and produces a response.
/// </summary>
/// <typeparam name="TResponse">The type produced by the handler.</typeparam>
#pragma warning disable CA1040 // The marker is required for compile-time handler resolution.
public interface IRequest<out TResponse>;
#pragma warning restore CA1040

/// <summary>
/// A request that changes state.
/// </summary>
/// <typeparam name="TResponse">The type produced by the handler.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>
/// A request that reads state without changing it.
/// </summary>
/// <typeparam name="TResponse">The type produced by the handler.</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>;

/// <summary>
/// Handles exactly one kind of request.
/// </summary>
/// <typeparam name="TRequest">The request type handled.</typeparam>
/// <typeparam name="TResponse">The type produced by the handler.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Executes the use case.</summary>
    /// <param name="request">The request to execute.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The response produced by the use case.</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Invokes the next step of the request pipeline.
/// </summary>
/// <typeparam name="TResponse">The type produced by the pipeline.</typeparam>
/// <returns>The response produced by the remainder of the pipeline.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// A cross-cutting concern that wraps the execution of every matching request.
/// </summary>
/// <typeparam name="TRequest">The request type the behaviour applies to.</typeparam>
/// <typeparam name="TResponse">The type produced by the pipeline.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Wraps the remainder of the pipeline.</summary>
    /// <param name="request">The request being executed.</param>
    /// <param name="next">The remainder of the pipeline.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The response produced by the pipeline.</returns>
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

/// <summary>
/// Dispatches a request to its handler through the configured pipeline.
/// </summary>
public interface ISender
{
    /// <summary>Dispatches a request.</summary>
    /// <typeparam name="TResponse">The type produced by the handler.</typeparam>
    /// <param name="request">The request to dispatch.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The response produced by the handler.</returns>
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
