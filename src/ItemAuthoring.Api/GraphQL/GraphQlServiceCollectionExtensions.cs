using ItemAuthoring.Api.GraphQL.DataLoaders;
using ItemAuthoring.Application.Abstractions.Events;
using ItemAuthoring.Domain.Items.Events;
using Serilog;
using Serilog.Extensions.Logging;

namespace ItemAuthoring.Api.GraphQL;

/// <summary>
/// Registers the GraphQL surface with the dependency injection container.
/// </summary>
public static class GraphQlServiceCollectionExtensions
{
    /// <summary>The maximum nesting depth an operation may request.</summary>
    public const int MaxExecutionDepth = 12;

    /// <summary>Registers the schema, its middleware and its safety limits.</summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddGraphQlApi(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddScoped<IDomainEventHandler<ItemPublishedDomainEvent>,
            ItemPublishedSubscriptionPublisher>();

        // The schema container is built before the host's logging pipeline is available, so the
        // filter's logger is created from the Serilog root logger the host also writes to.
        var loggerFactory = new SerilogLoggerFactory(Log.Logger, dispose: false);

        services
            .AddGraphQLServer()
            // Hot Chocolate activates schema-level services from its own container, so the error
            // filter's dependencies are registered there explicitly rather than relying on the host.
            .ConfigureSchemaServices(schemaServices =>
            {
                schemaServices.AddSingleton(loggerFactory.CreateLogger<GraphQlErrorFilter>());
                schemaServices.AddSingleton(environment);
            })
            .AddAuthorization()
            .AddInMemorySubscriptions()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddSubscriptionType<Subscription>()
            .AddTypeExtension<ExamMutation>()
            .AddTypeExtension<AdministrationMutation>()
            .AddTypeExtension<ItemSummaryTypeExtensions>()
            .AddTypeExtension<ExamSummaryTypeExtensions>()
            .AddTypeExtension<ExamItemTypeExtensions>()
            .AddTypeExtension<UserTypeExtensions>()
            .AddDataLoader<CategoryByIdDataLoader>()
            .AddDataLoader<TagsByItemDataLoader>()
            .AddDataLoader<OptionsByItemDataLoader>()
            .AddDataLoader<SectionsByExamDataLoader>()
            .AddDataLoader<RolesByUserDataLoader>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddErrorFilter<GraphQlErrorFilter>()
            // A depth limit is the cheapest defence against the recursive query that turns a public
            // GraphQL endpoint into a denial of service vector.
            .AddMaxExecutionDepthRule(MaxExecutionDepth, skipIntrospectionFields: true)
            .ModifyRequestOptions(options =>
            {
                options.IncludeExceptionDetails = environment.IsDevelopment();
            });

        return services;
    }
}
