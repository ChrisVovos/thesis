namespace ItemAuthoring.Domain.Common;

/// <summary>
/// Contract exposed to the persistence layer so that raised domain events can be collected after a
/// successful <c>SaveChanges</c> without the infrastructure knowing the concrete aggregate type.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>Gets the events raised by this aggregate during the current unit of work.</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>Removes every recorded event after they have been handed to the dispatcher.</summary>
    void ClearDomainEvents();
}
