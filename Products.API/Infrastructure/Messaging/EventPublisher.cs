using Microsoft.Extensions.Logging;
using Products.API.Application.Interfaces;
using Shared.Messaging; // Use shared interface

namespace Products.API.Infrastructure.Messaging;

/// <summary>
/// Default event publisher implementation that delegates to the shared message bus.
/// Provides a seam for adding reliability features later (outbox, retries, tracing).
/// </summary>
/// <param name="messageBus">Underlying message bus</param>
/// <param name="logger">Logger instance</param>
public class EventPublisher(
    IMessageBus messageBus, // Now uses Shared.Messaging.IMessageBus
    ILogger<EventPublisher> logger) : IEventPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = typeof(TEvent).Name;
        logger.LogDebug("Publishing event {EventType}: {@Event}", eventType, @event);

        await messageBus.PublishAsync(@event, cancellationToken);

        logger.LogDebug("Successfully published event {EventType}", eventType);
    }
}