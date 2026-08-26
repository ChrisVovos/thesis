namespace ItemAuthoring.Application.Abstractions.Persistence;

/// <summary>
/// An explicit database transaction.
/// </summary>
public interface ITransactionScope : IAsyncDisposable
{
    /// <summary>Commits the transaction.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls the transaction back.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

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
/// <see cref="BeginTransactionAsync"/> is used only by the two use cases that genuinely span more
/// than one aggregate — assembling an exam from bank items, and rotating a refresh token while
/// updating the owning user. Everywhere else, saving once is already atomic.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>Persists every change tracked for the current request.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Starts an explicit transaction spanning several aggregates.</summary>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The transaction scope.</returns>
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
