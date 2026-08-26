using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Abstractions.Time;
using ItemAuthoring.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ItemAuthoring.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps the audit columns of every aggregate that is being inserted or updated.
/// </summary>
/// <remarks>
/// Doing this in an interceptor rather than in each aggregate keeps the clock and the current
/// principal out of the domain, so aggregate behaviour remains deterministic and unit testable
/// without a time abstraction threaded through every method signature.
/// </remarks>
/// <param name="clock">The clock supplying the audit timestamps.</param>
/// <param name="currentUser">The principal on whose behalf the request executes.</param>
internal sealed class AuditableEntityInterceptor(IClock clock, ICurrentUser currentUser)
    : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.UtcNow;
        var actingUser = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.MarkCreated(now, actingUser);
                    break;

                case EntityState.Modified:
                    entry.Entity.MarkModified(now, actingUser);
                    break;

                default:
                    break;
            }
        }
    }
}
