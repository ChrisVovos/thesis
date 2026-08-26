namespace ItemAuthoring.Application.Abstractions.Diagnostics;

/// <summary>
/// Counts the database commands executed while handling the current request.
/// </summary>
/// <remarks>
/// The number of round trips a request causes is one of the three quantities the comparative study
/// reports, and it is the one an HTTP client cannot observe. Counting it server side, in a scope tied
/// to the request, is the only way to attribute it correctly to REST or to GraphQL.
/// </remarks>
public interface IDatabaseCommandCounter
{
    /// <summary>Gets the number of database commands executed so far in this request.</summary>
    int Count { get; }

    /// <summary>Records that a database command was executed.</summary>
    void Increment();
}
