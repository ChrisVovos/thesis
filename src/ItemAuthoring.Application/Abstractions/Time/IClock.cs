namespace ItemAuthoring.Application.Abstractions.Time;

/// <summary>
/// Supplies the current instant.
/// </summary>
/// <remarks>
/// Handlers depend on this rather than on <see cref="DateTimeOffset.UtcNow"/> so that lifecycle
/// rules involving expiry, lockout and publication can be tested without waiting for real time.
/// </remarks>
public interface IClock
{
    /// <summary>Gets the current instant in UTC.</summary>
    DateTimeOffset UtcNow { get; }
}
