using System.Data.Common;
using ItemAuthoring.Application.Abstractions.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ItemAuthoring.Infrastructure.Persistence.Interceptors;

/// <summary>
/// A request scoped counter of executed database commands.
/// </summary>
internal sealed class DatabaseCommandCounter : IDatabaseCommandCounter
{
    private int _count;

    /// <inheritdoc />
    public int Count => Volatile.Read(ref _count);

    /// <inheritdoc />
    public void Increment() => Interlocked.Increment(ref _count);
}

/// <summary>
/// Feeds the request scoped <see cref="IDatabaseCommandCounter"/> from Entity Framework Core.
/// </summary>
/// <param name="counter">The counter for the current request.</param>
internal sealed class CommandCountingInterceptor(IDatabaseCommandCounter counter) : DbCommandInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        counter.Increment();
        return base.ReaderExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        counter.Increment();
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        counter.Increment();
        return base.NonQueryExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        counter.Increment();
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        counter.Increment();
        return base.ScalarExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        counter.Increment();
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
}
