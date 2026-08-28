namespace ItemAuthoring.Application.Abstractions.Persistence;

/// <summary>
/// Commits the changes tracked for the current request.
/// </summary>
/// <remarks>
/// <para>
/// <c>DbContext</c> already is a unit of work, and wrapping it in a second one purely to satisfy a
/// pattern checklist would add indirection without adding behaviour. This interface exists for one
/// reason only: to keep the application layer free of a reference to Entity Framework Core.
/// </para>
/// <para>
/// <see cref="ExecuteInTransactionAsync"/> is used only by the two use cases that genuinely span more
/// than one aggregate — publishing an exam assembled from bank items, and rotating a refresh token
/// while updating the owning user. Everywhere else, saving once is already atomic. It takes the work
/// as a delegate rather than handing out a transaction the caller drives, because the provider is
/// configured to retry transient failures and can only retry an operation it is able to run again
/// from the beginning.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>Persists every change tracked for the current request.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs an operation spanning several aggregates as one retriable transaction.</summary>
    /// <typeparam name="TResult">The result the operation produces.</typeparam>
    /// <param name="operation">The work to perform inside the transaction.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The result of the operation.</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
