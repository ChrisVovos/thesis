using System.Reflection;
using FluentValidation;
using ItemAuthoring.Application.Abstractions.Messaging;
using ItemAuthoring.Application.Abstractions.Security;
using ItemAuthoring.Application.Behaviors;
using ItemAuthoring.Application.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ItemAuthoring.Application;

/// <summary>
/// Registers the application layer with the dependency injection container.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Registers the dispatcher, every request handler, every validator and the pipeline.</summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        services.AddScoped<ISender, Sender>();
        services.AddScoped<IPermissionGuard, PermissionGuard>();
        services.AddClosedGenericImplementations(assembly, typeof(IRequestHandler<,>));
        services.AddClosedGenericImplementations(assembly, typeof(IValidator<>));

        // Order matters and reads outside-in: authorize, then validate, then log, and only then let
        // the handler run inside the domain-exception translator that is closest to it.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(DomainExceptionBehavior<,>));

        return services;
    }

    private static void AddClosedGenericImplementations(
        this IServiceCollection services,
        Assembly assembly,
        Type openGenericContract)
    {
        var registrations =
            from type in assembly.GetTypes()
            where type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false }
            from contract in type.GetInterfaces()
            where contract.IsGenericType
                && contract.GetGenericTypeDefinition() == openGenericContract
            select (Contract: contract, Implementation: type);

        foreach (var registration in registrations)
        {
            services.AddScoped(registration.Contract, registration.Implementation);
        }
    }
}
