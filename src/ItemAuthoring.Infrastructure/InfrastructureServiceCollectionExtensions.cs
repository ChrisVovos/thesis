using ItemAuthoring.Application.Abstractions.Diagnostics;
using ItemAuthoring.Application.Abstractions.Events;
using ItemAuthoring.Application.Abstractions.Persistence;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Abstractions.Time;
using ItemAuthoring.Infrastructure.Events;
using ItemAuthoring.Infrastructure.Identity;
using ItemAuthoring.Infrastructure.Persistence;
using ItemAuthoring.Infrastructure.Persistence.Interceptors;
using ItemAuthoring.Infrastructure.Persistence.Query;
using ItemAuthoring.Infrastructure.Persistence.ReadStores;
using ItemAuthoring.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ItemAuthoring.Infrastructure;

/// <summary>
/// Registers the infrastructure layer with the dependency injection container.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>The configuration key holding the SQL Server connection string.</summary>
    public const string ConnectionStringName = "ItemAuthoringDatabase";

    /// <summary>Registers persistence, identity and supporting services.</summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="InvalidOperationException">The connection string is missing.</exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"The connection string '{ConnectionStringName}' is not configured.");

        services.AddOptionsWithValidation<JwtOptions>(configuration, JwtOptions.SectionName);
        services.AddOptionsWithValidation<SeedOptions>(configuration, SeedOptions.SectionName);

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IDatabaseCommandCounter, DatabaseCommandCounter>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<CommandCountingInterceptor>();

        services.AddDbContext<ApplicationDbContext>((provider, builder) => builder
            .UseSqlServer(connectionString, sqlServer => sqlServer
                .EnableRetryOnFailure(maxRetryCount: 3, TimeSpan.FromSeconds(5), null)
                .MigrationsHistoryTable("__EFMigrationsHistory", "authoring"))
            .UseValueObjectMemberTranslation()
            .AddInterceptors(
                provider.GetRequiredService<AuditableEntityInterceptor>(),
                provider.GetRequiredService<CommandCountingInterceptor>()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAsyncQueryExecutor, EntityFrameworkQueryExecutor>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IExamRepository, ExamRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();

        services.AddScoped<IItemReadStore, ItemReadStore>();
        services.AddScoped<ITaxonomyReadStore, TaxonomyReadStore>();
        services.AddScoped<IExamReadStore, ExamReadStore>();
        services.AddScoped<IIdentityReadStore, IdentityReadStore>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    private static void AddOptionsWithValidation<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
        => services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
