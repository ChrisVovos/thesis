using ItemAuthoring.Application.Abstractions.Events;
using ItemAuthoring.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ItemAuthoring.Infrastructure.Events;

/// <summary>
/// Resolves and invokes the handlers registered for each raised domain event.
/// </summary>
/// <remarks>
/// A failing handler is logged and swallowed rather than propagated. The transaction has already
/// committed by the time dispatch happens, so throwing here would report failure for an operation
/// that actually succeeded. Handlers that must not be lost belong on a durable queue, which is the
/// natural extension point once this application grows past a single process.
/// </remarks>
/// <param name="serviceProvider">The scope from which handlers are resolved.</param>
/// <param name="logger">The logger.</param>
internal sealed class DomainEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<DomainEventDispatcher> logger)
    : IDomainEventDispatcher
{
    /// <inheritdoc />
    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                await InvokeAsync(handler, handlerType, domainEvent, cancellationToken);
            }
        }
    }

    private async Task InvokeAsync(
        object? handler,
        Type handlerType,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        if (handler is null)
        {
            return;
        }

        try
        {
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
            await (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "The handler {HandlerType} failed while processing {EventType}.",
                handler.GetType().Name,
                domainEvent.GetType().Name);
        }
    }
}
