using Orders.API.Infrastructure.Messaging;
using Shared.Domain.Events;
using Shared.Messaging; // Add this using directive

namespace Orders.API.Infrastructure.Messaging.Extensions;

/// <summary>
/// Extension methods for configuring message bus services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds in-memory message bus as a singleton service
    /// This is suitable for development, testing, and single-instance scenarios
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddInMemoryMessageBus(this IServiceCollection services)
    {
        // Register as singleton to maintain handlers across application lifetime
        // This ensures that event subscriptions persist throughout the application lifecycle
        services.AddSingleton<Shared.Messaging.IMessageBus, InMemoryMessageBus>();
        
        return services;
    }

    /// <summary>
    /// Adds message bus with custom implementation
    /// Allows for flexibility in choosing different message bus implementations
    /// </summary>
    /// <typeparam name="TImplementation">Implementation type that implements IMessageBus</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="serviceLifetime">Service lifetime (default: Singleton for message buses)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddMessageBus<TImplementation>(
        this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        where TImplementation : class, Shared.Messaging.IMessageBus
    {
        services.Add(new ServiceDescriptor(typeof(Shared.Messaging.IMessageBus), typeof(TImplementation), serviceLifetime));
        return services;
    }

    /// <summary>
    /// Adds message bus with factory pattern for complex initialization scenarios
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="factory">Factory function to create message bus instance</param>
    /// <param name="serviceLifetime">Service lifetime (default: Singleton)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddMessageBus(
        this IServiceCollection services,
        Func<IServiceProvider, Shared.Messaging.IMessageBus> factory,
        ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
    {
        services.Add(new ServiceDescriptor(typeof(Shared.Messaging.IMessageBus), factory, serviceLifetime));
        return services;
    }

    /// <summary>
    /// Registers event handlers and subscribes them to the message bus
    /// This sets up cross-service event handling
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEventHandlers(this IServiceCollection services)
    {
        // Register event handlers as scoped services
        services.AddScoped<ProductPriceChangedEventHandler>();
        
        return services;
    }

    /// <summary>
    /// Configures message bus subscriptions for event handlers
    /// This method should be called after the service provider is built
    /// </summary>
    /// <param name="serviceProvider">Service provider to resolve dependencies</param>
    /// <returns>Service provider for chaining</returns>
    public static IServiceProvider ConfigureMessageBusSubscriptions(this IServiceProvider serviceProvider)
    {
        var messageBus = serviceProvider.GetRequiredService<Shared.Messaging.IMessageBus>();
        var logger = serviceProvider.GetRequiredService<ILogger<IServiceProvider>>();
        
        logger.LogInformation("Configuring message bus subscriptions...");

        // Subscribe ProductPriceChangedEventHandler to ProductPriceChangedEvent
        messageBus.Subscribe<ProductPriceChangedEvent>(async (@event) =>
        {
            using var scope = serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ProductPriceChangedEventHandler>();
            await handler.HandleAsync(@event);
        });

        logger.LogInformation("Successfully configured message bus subscriptions for Orders.API");
        
        return serviceProvider;
    }
}