using ItemAuthoring.Domain.Common;

namespace ItemAuthoring.Application.Abstractions.Events;

/// <summary>
/// Handles a domain event after the transaction that raised it has committed.
/// </summary>
/// <typeparam name="TEvent">The event handled.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>Reacts to the event.</summary>
    /// <param name="domainEvent">The event that occurred.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Delivers raised domain events to their handlers.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>Delivers a batch of events.</summary>
    /// <param name="domainEvents">The events to deliver.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken);
}
