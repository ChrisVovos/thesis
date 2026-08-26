using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Time;
using Microsoft.EntityFrameworkCore;

namespace ItemAuthoring.Infrastructure.Persistence;

/// <summary>
/// Materializes composed queries through the Entity Framework Core asynchronous operators.
/// </summary>
internal sealed class EntityFrameworkQueryExecutor : IAsyncQueryExecutor
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> ToListAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default)
        => await query.ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default)
        => query.CountAsync(cancellationToken);

    /// <inheritdoc />
    public Task<T?> FirstOrDefaultAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default)
        => query.FirstOrDefaultAsync(cancellationToken);
}

/// <summary>
/// The system clock.
/// </summary>
internal sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
