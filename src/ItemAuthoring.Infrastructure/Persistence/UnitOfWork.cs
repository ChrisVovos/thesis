using ItemAuthoring.Application.Abstractions.Events;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ItemAuthoring.Infrastructure.Persistence;

/// <summary>
/// Commits the changes tracked by <see cref="ApplicationDbContext"/> and then publishes the domain
/// events raised while they were made.
/// </summary>
/// <remarks>
/// The ordering is the whole point. Events are collected before the write and dispatched after it, so
/// a handler can never observe a fact that was subsequently rolled back, and an event is never lost
/// because the aggregate was detached before dispatch.
/// </remarks>
/// <param name="context">The Entity Framework Core session.</param>
/// <param name="dispatcher">The domain event dispatcher.</param>
internal sealed class UnitOfWork(ApplicationDbContext context, IDomainEventDispatcher dispatcher)
    : IUnitOfWork
{
    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = CollectDomainEvents();
        var written = await context.SaveChangesAsync(cancellationToken);

        if (domainEvents.Count > 0)
        {
            await dispatcher.DispatchAsync(domainEvents, cancellationToken);
        }

        return written;
    }

    /// <inheritdoc />
    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // A caller further up the stack already owns the transaction and its outcome.
        if (context.Database.CurrentTransaction is not null)
        {
            return operation(cancellationToken);
        }

        return context.Database.CreateExecutionStrategy().ExecuteAsync(
            async token =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(token);
                var result = await operation(token);
                await transaction.CommitAsync(token);
                return result;
            },
            cancellationToken);
    }

    private List<IDomainEvent> CollectDomainEvents()
    {
        var aggregates = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        var events = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();
        aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());
        return events;
    }
}
