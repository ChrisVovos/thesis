using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

namespace ItemAuthoring.Infrastructure.Persistence.Query;

/// <summary>
/// Teaches Entity Framework Core to translate the single wrapped value of a value object into the
/// column that stores it.
/// </summary>
/// <remarks>
/// <para>
/// Strongly typed identifiers and single-value value objects are mapped with a
/// <see cref="ValueConverter"/>. Out of the box Entity Framework Core knows how to read and write
/// such a property, but it cannot translate a member access on it: <c>item.Id.Value</c> has no
/// meaning in SQL as far as the query pipeline is concerned. That is only tolerable while the access
/// sits in the outermost projection, where it can be evaluated on the client. The read stores of this
/// application deliberately return a composable <see cref="IQueryable{T}"/> over an already projected
/// data transfer object, so every one of those accesses becomes an inner expression the moment a
/// caller appends a filter or an ordering — and the query then fails to translate.
/// </para>
/// <para>
/// Rather than give up either the value objects or the composable read model, this translator closes
/// the gap: whenever the accessed member is exactly the member the converter itself reads, the access
/// is rewritten to the underlying column. Filtering and sorting therefore reach SQL Server for both
/// API surfaces, which is the property the comparative study depends on.
/// </para>
/// </remarks>
/// <param name="sqlExpressionFactory">Builds the replacement expression.</param>
/// <param name="typeMappingSource">Resolves the mapping of the underlying column type.</param>
internal sealed class ValueObjectMemberTranslator(
    ISqlExpressionFactory sqlExpressionFactory,
    IRelationalTypeMappingSource typeMappingSource)
    : IMemberTranslator
{
    /// <inheritdoc />
    public SqlExpression? Translate(
        SqlExpression? instance,
        MemberInfo member,
        Type returnType,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance?.TypeMapping is not { Converter: { } converter } mapping
            || converter.ProviderClrType != returnType
            || !ReadsSameMember(converter, member))
        {
            return null;
        }

        var providerMapping = typeMappingSource.FindMapping(returnType, mapping.StoreType)
            ?? typeMappingSource.FindMapping(returnType);

        return providerMapping is null
            ? null
            : sqlExpressionFactory.Convert(instance, returnType, providerMapping);
    }

    private static bool ReadsSameMember(ValueConverter converter, MemberInfo member)
    {
        if (Unwrap(converter.ConvertToProviderExpression.Body) is not MemberExpression access
            || access.Member.Name != member.Name)
        {
            return false;
        }

        // The generic identifier converter reads the value through the interface, so the member the
        // caller wrote and the member the converter reads are declared on different types.
        var declaringType = access.Member.DeclaringType;
        return Unwrap(access.Expression) is ParameterExpression
            && (declaringType == member.DeclaringType
                || member.DeclaringType?.IsAssignableTo(declaringType) == true);
    }

    private static Expression? Unwrap(Expression? expression)
    {
        while (expression is UnaryExpression
               { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } cast)
        {
            expression = cast.Operand;
        }

        return expression;
    }
}

/// <summary>Exposes <see cref="ValueObjectMemberTranslator"/> to the query pipeline.</summary>
/// <param name="sqlExpressionFactory">Builds the replacement expression.</param>
/// <param name="typeMappingSource">Resolves the mapping of the underlying column type.</param>
internal sealed class ValueObjectMemberTranslatorPlugin(
    ISqlExpressionFactory sqlExpressionFactory,
    IRelationalTypeMappingSource typeMappingSource)
    : IMemberTranslatorPlugin
{
    /// <inheritdoc />
    public IEnumerable<IMemberTranslator> Translators { get; } =
        [new ValueObjectMemberTranslator(sqlExpressionFactory, typeMappingSource)];
}

/// <summary>Registers <see cref="ValueObjectMemberTranslatorPlugin"/> with a context.</summary>
internal sealed class ValueObjectQueryOptionsExtension : IDbContextOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;

    /// <inheritdoc />
    public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
        => services.AddScoped<IMemberTranslatorPlugin, ValueObjectMemberTranslatorPlugin>();

    /// <inheritdoc />
    public void Validate(IDbContextOptions options)
    {
    }

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension)
        : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "using value object member translation ";

        public override int GetServiceProviderHashCode() => 0;

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            ArgumentNullException.ThrowIfNull(debugInfo);
            debugInfo["ValueObjects:MemberTranslation"] = "1";
        }
    }
}

/// <summary>Configuration entry point for value object query translation.</summary>
internal static class ValueObjectQueryOptionsBuilderExtensions
{
    /// <summary>
    /// Enables translation of value object member access into the column that stores the value.
    /// </summary>
    /// <param name="builder">The options being configured.</param>
    /// <returns>The same builder, so calls can be chained.</returns>
    public static DbContextOptionsBuilder UseValueObjectMemberTranslation(
        this DbContextOptionsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ((IDbContextOptionsBuilderInfrastructure)builder)
            .AddOrUpdateExtension(new ValueObjectQueryOptionsExtension());
        return builder;
    }
}
