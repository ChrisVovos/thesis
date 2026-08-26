namespace ItemAuthoring.Domain.Common;

/// <summary>
/// Raised when an operation would leave an aggregate in a state that violates a domain invariant.
/// </summary>
/// <remarks>
/// The <see cref="Code"/> is a stable, machine readable identifier. It is the mechanism that keeps
/// REST and GraphQL error responses in lockstep: both surfaces publish the same code for the same
/// rule violation, which is a prerequisite for the API comparison to be meaningful.
/// </remarks>
public sealed class DomainException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="DomainException"/> class.</summary>
    /// <param name="code">The stable, machine readable rule identifier.</param>
    /// <param name="message">The human readable explanation of the violated rule.</param>
    public DomainException(string code, string message)
        : base(message) => Code = code;

    /// <summary>Initializes a new instance of the <see cref="DomainException"/> class.</summary>
    /// <param name="message">The human readable explanation of the violated rule.</param>
    public DomainException(string message)
        : this("domain.rule_violation", message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DomainException"/> class.</summary>
    public DomainException()
        : this("domain.rule_violation", "A domain rule was violated.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DomainException"/> class.</summary>
    /// <param name="message">The human readable explanation of the violated rule.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException) => Code = "domain.rule_violation";

    /// <summary>Gets the stable, machine readable rule identifier.</summary>
    public string Code { get; } = "domain.rule_violation";
}
