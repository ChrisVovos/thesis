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
    public async Task<ITransactionScope> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction is { } existing)
        {
            return new AmbientTransactionScope(existing);
        }

        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new EntityFrameworkTransactionScope(transaction);
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

    private sealed class EntityFrameworkTransactionScope(IDbContextTransaction transaction)
        : ITransactionScope
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
            => transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default)
            => transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }

    /// <summary>
    /// Joins a transaction that a caller further up the stack already owns.
    /// </summary>
    /// <remarks>
    /// Nested calls must not commit or roll back a transaction they did not start; the outermost
    /// scope remains responsible for the outcome.
    /// </remarks>
    private sealed class AmbientTransactionScope(IDbContextTransaction transaction) : ITransactionScope
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken = default)
            => transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
