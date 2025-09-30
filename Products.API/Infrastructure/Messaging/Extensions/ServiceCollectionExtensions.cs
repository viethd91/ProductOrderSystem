using Shared.Messaging; // Use shared interface

namespace Products.API.Infrastructure.Messaging.Extensions;

/// <summary>
/// Extension methods for configuring message bus services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds in-memory message bus as a singleton service
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddInMemoryMessageBus(this IServiceCollection services)
    {
        // Register as singleton to maintain handlers across application lifetime
        services.AddSingleton<Shared.Messaging.IMessageBus, InMemoryMessageBus>();
        
        return services;
    }

    /// <summary>
    /// Adds message bus with custom implementation
    /// </summary>
    /// <typeparam name="TImplementation">Implementation type</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="serviceLifetime">Service lifetime (default: Singleton)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddMessageBus<TImplementation>(
        this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        where TImplementation : class, Shared.Messaging.IMessageBus
    {
        services.Add(new ServiceDescriptor(typeof(Shared.Messaging.IMessageBus), typeof(TImplementation), serviceLifetime));
        return services;
    }
}