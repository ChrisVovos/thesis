using HotChocolate;
using ItemAuthoring.Api.Common;
using ItemAuthoring.Application.Common;
using Error = ItemAuthoring.Application.Common.Error;

namespace ItemAuthoring.Api.GraphQL;

/// <summary>
/// Translates an application <see cref="Result"/> into the GraphQL error contract.
/// </summary>
/// <remarks>
/// A GraphQL resolver signals failure by throwing, but the error it throws carries exactly the code
/// and classification the REST surface would have returned in its problem document. A client can
/// therefore branch on <c>extensions.code</c> over GraphQL and on the <c>code</c> member of the
/// problem document over REST, and see the same value for the same failure.
/// </remarks>
public static class GraphQlResultExtensions
{
    /// <summary>The error extension carrying the stable failure code.</summary>
    public const string CodeExtension = "code";

    /// <summary>The error extension carrying the transport-neutral classification.</summary>
    public const string ClassificationExtension = "classification";

    /// <summary>Returns the value of a successful result, or throws the equivalent GraphQL error.</summary>
    /// <typeparam name="TValue">The type carried by a successful result.</typeparam>
    /// <param name="result">The outcome of the use case.</param>
    /// <returns>The produced value.</returns>
    /// <exception cref="GraphQLException">The use case failed.</exception>
    public static TValue UnwrapOrThrow<TValue>(this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? result.Value : throw new GraphQLException(ToError(result.Error));
    }

    /// <summary>Confirms a successful result, or throws the equivalent GraphQL error.</summary>
    /// <param name="result">The outcome of the use case.</param>
    /// <returns><see langword="true"/> when the use case succeeded.</returns>
    /// <exception cref="GraphQLException">The use case failed.</exception>
    public static bool UnwrapOrThrow(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? true : throw new GraphQLException(ToError(result.Error));
    }

    /// <summary>Converts an application error into a GraphQL error.</summary>
    /// <param name="error">The application error.</param>
    /// <returns>The GraphQL error.</returns>
    public static IError ToError(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var builder = ErrorBuilder.New()
            .SetMessage(error.Message)
            .SetCode(error.Code)
            .SetExtension(CodeExtension, error.Code)
            .SetExtension(
                ClassificationExtension,
                ErrorStatusMap.ToGraphQlClassification(error.Type));

        return error.Details is { Count: > 0 } details
            ? builder.SetExtension("errors", ToExtensionValue(details)).Build()
            : builder.Build();
    }

    // The result formatter writes dictionaries and lists of object, and falls back to ToString for
    // anything else, which would put the type name of the dictionary on the wire.
    private static Dictionary<string, object?> ToExtensionValue(
        IReadOnlyDictionary<string, string[]> details)
        => details.ToDictionary(
            entry => entry.Key,
            entry => (object?)entry.Value.Select(message => (object?)message).ToList());
}
