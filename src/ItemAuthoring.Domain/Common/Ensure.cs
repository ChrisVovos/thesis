namespace ItemAuthoring.Domain.Common;

/// <summary>
/// Guard clauses used by value objects and aggregates to enforce invariants at the point of change.
/// </summary>
public static class Ensure
{
    /// <summary>Fails when the supplied text is <see langword="null"/>, empty or white space.</summary>
    /// <param name="value">The text to validate.</param>
    /// <param name="code">The stable rule identifier reported on failure.</param>
    /// <param name="message">The human readable explanation reported on failure.</param>
    /// <returns>The trimmed text.</returns>
    /// <exception cref="DomainException">The text was missing.</exception>
    public static string NotBlank(string? value, string code, string message)
        => string.IsNullOrWhiteSpace(value) ? throw new DomainException(code, message) : value.Trim();

    /// <summary>Fails when the supplied text exceeds <paramref name="maxLength"/> characters.</summary>
    /// <param name="value">The text to validate.</param>
    /// <param name="maxLength">The inclusive maximum length.</param>
    /// <param name="code">The stable rule identifier reported on failure.</param>
    /// <param name="message">The human readable explanation reported on failure.</param>
    /// <returns>The validated text.</returns>
    /// <exception cref="DomainException">The text was too long.</exception>
    public static string MaxLength(string value, int maxLength, string code, string message)
        => value.Length > maxLength ? throw new DomainException(code, message) : value;

    /// <summary>Fails when <paramref name="condition"/> does not hold.</summary>
    /// <param name="condition">The invariant that must hold.</param>
    /// <param name="code">The stable rule identifier reported on failure.</param>
    /// <param name="message">The human readable explanation reported on failure.</param>
    /// <exception cref="DomainException">The invariant did not hold.</exception>
    public static void That(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }

    /// <summary>Fails when the supplied reference is <see langword="null"/>.</summary>
    /// <typeparam name="T">The reference type being validated.</typeparam>
    /// <param name="value">The reference to validate.</param>
    /// <param name="code">The stable rule identifier reported on failure.</param>
    /// <param name="message">The human readable explanation reported on failure.</param>
    /// <returns>The non-null reference.</returns>
    /// <exception cref="DomainException">The reference was <see langword="null"/>.</exception>
    public static T NotNull<T>(T? value, string code, string message)
        where T : class
        => value ?? throw new DomainException(code, message);
}
