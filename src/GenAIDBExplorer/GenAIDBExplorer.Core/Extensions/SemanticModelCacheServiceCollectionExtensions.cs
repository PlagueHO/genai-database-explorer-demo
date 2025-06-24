using GenAIDBExplorer.Core.SemanticModelProviders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace GenAIDBExplorer.Core.Extensions;

/// <summary>
/// Extensions for configuring semantic model caching services.
/// </summary>
public static class SemanticModelCacheServiceCollectionExtensions
{
    /// <summary>
    /// Adds semantic model caching services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSemanticModelCaching(this IServiceCollection services)
    {
        // Add memory cache if not already registered
        services.AddMemoryCache();

        // Register the cache implementation
        services.AddSingleton<ISemanticModelCache, InMemorySemanticModelCache>();

        // Register the cached schema repository decorator
        services.Decorate<ISchemaRepository, CachedSchemaRepository>();

        return services;
    }

    /// <summary>
    /// Adds semantic model caching services to the service collection with custom memory cache options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure memory cache options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSemanticModelCaching(this IServiceCollection services, Action<MemoryCacheOptions> configureOptions)
    {
        // Add memory cache with custom options
        services.AddMemoryCache(configureOptions);

        // Register the cache implementation
        services.AddSingleton<ISemanticModelCache, InMemorySemanticModelCache>();

        // Register the cached schema repository decorator
        services.Decorate<ISchemaRepository, CachedSchemaRepository>();

        return services;
    }
}

/// <summary>
/// Service collection extensions for decorating services.
/// </summary>
public static class ServiceCollectionDecoratorExtensions
{
    /// <summary>
    /// Decorates a service registration with a decorator implementation.
    /// </summary>
    /// <typeparam name="TInterface">The service interface type.</typeparam>
    /// <typeparam name="TDecorator">The decorator implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection Decorate<TInterface, TDecorator>(this IServiceCollection services)
        where TInterface : class
        where TDecorator : class, TInterface
    {
        // Find the existing service registration
        var existingService = services.LastOrDefault(s => s.ServiceType == typeof(TInterface));
        if (existingService == null)
        {
            throw new InvalidOperationException($"Service of type {typeof(TInterface).Name} is not registered. Register the service before decorating it.");
        }

        // Remove the existing registration
        services.Remove(existingService);

        // Add the original service with a different name
        services.Add(ServiceDescriptor.Describe(
            typeof(TInterface),
            provider =>
            {
                if (existingService.ImplementationInstance != null)
                {
                    return existingService.ImplementationInstance;
                }
                if (existingService.ImplementationFactory != null)
                {
                    return existingService.ImplementationFactory(provider);
                }
                if (existingService.ImplementationType != null)
                {
                    return ActivatorUtilities.CreateInstance(provider, existingService.ImplementationType);
                }
                throw new InvalidOperationException("Invalid service descriptor");
            },
            existingService.Lifetime));

        // Register the decorator
        services.Add(ServiceDescriptor.Describe(
            typeof(TInterface),
            provider =>
            {
                var decoratedServices = provider.GetServices<TInterface>().ToList();
                var originalService = decoratedServices.FirstOrDefault(s => s.GetType() != typeof(TDecorator));
                if (originalService == null)
                {
                    throw new InvalidOperationException($"Could not find original service implementation for {typeof(TInterface).Name}");
                }
                return ActivatorUtilities.CreateInstance<TDecorator>(provider, originalService);
            },
            existingService.Lifetime));

        return services;
    }
}