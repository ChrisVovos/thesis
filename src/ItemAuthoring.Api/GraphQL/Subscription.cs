using HotChocolate;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using ItemAuthoring.Application.Abstractions.Events;
using ItemAuthoring.Domain.Items.Events;

namespace ItemAuthoring.Api.GraphQL;

/// <summary>An item has been published and is now available to exam builders.</summary>
/// <param name="ItemId">The identity of the item.</param>
/// <param name="VersionNumber">The version number that was frozen.</param>
/// <param name="PublishedAtUtc">The publication instant.</param>
public sealed record ItemPublishedMessage(
    Guid ItemId,
    int VersionNumber,
    DateTimeOffset PublishedAtUtc);

/// <summary>
/// The root subscription type.
/// </summary>
/// <remarks>
/// Real-time notification is the one capability REST cannot match without a second protocol. It is
/// included here because the item bank genuinely benefits from it — an exam builder wants to see new
/// material appear without polling — and because it is a substantive point of comparison rather than
/// a feature added for its own sake.
/// </remarks>
public sealed class Subscription
{
    /// <summary>Emits an event whenever an item is published.</summary>
    /// <param name="message">The published message.</param>
    /// <returns>The message delivered to the subscriber.</returns>
    [Subscribe]
    [Topic(nameof(OnItemPublished))]
    public ItemPublishedMessage OnItemPublished([EventMessage] ItemPublishedMessage message) => message;
}

/// <summary>
/// Bridges the domain event onto the GraphQL subscription topic.
/// </summary>
/// <remarks>
/// The domain raises <see cref="ItemPublishedDomainEvent"/> without knowing that a transport exists;
/// this handler is the only place that knows about both. Dispatch happens after the transaction has
/// committed, so a subscriber can never be told about a publication that was rolled back.
/// </remarks>
/// <param name="sender">The topic event sender.</param>
internal sealed class ItemPublishedSubscriptionPublisher(ITopicEventSender sender)
    : IDomainEventHandler<ItemPublishedDomainEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        ItemPublishedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        await sender.SendAsync(
            nameof(Subscription.OnItemPublished),
            new ItemPublishedMessage(
                domainEvent.ItemId.Value,
                domainEvent.VersionNumber,
                domainEvent.OccurredOnUtc),
            cancellationToken);
    }
}
