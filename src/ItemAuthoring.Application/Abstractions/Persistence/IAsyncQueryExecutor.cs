namespace ItemAuthoring.Application.Abstractions.Persistence;

/// <summary>
/// Materializes a composed <see cref="IQueryable{T}"/> asynchronously.
/// </summary>
/// <remarks>
/// <c>ToListAsync</c> and <c>CountAsync</c> are Entity Framework Core extension methods. Calling them
/// directly would put an infrastructure package reference into the application layer for the sake of
/// two method names. This interface keeps the dependency rule intact at negligible cost, and it makes
/// query handlers testable against an in-memory provider without a database.
/// </remarks>
public interface IAsyncQueryExecutor
{
    /// <summary>Materializes the query into a list.</summary>
    /// <typeparam name="T">The element type of the query.</typeparam>
    /// <param name="query">The composed query.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The materialized rows.</returns>
    Task<IReadOnlyList<T>> ToListAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default);

    /// <summary>Counts the rows matching the query.</summary>
    /// <typeparam name="T">The element type of the query.</typeparam>
    /// <param name="query">The composed query.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The number of matching rows.</returns>
    Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);

    /// <summary>Reads the first row matching the query, if any.</summary>
    /// <typeparam name="T">The element type of the query.</typeparam>
    /// <param name="query">The composed query.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The first row, or <see langword="null"/> when the query matched nothing.</returns>
    Task<T?> FirstOrDefaultAsync<T>(
        IQueryable<T> query,
        CancellationToken cancellationToken = default);
}
