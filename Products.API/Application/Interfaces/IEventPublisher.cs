namespace Products.API.Application.Interfaces;

/// <summary>
/// Abstraction for publishing integration/domain events from the application layer.
/// Wraps the underlying message bus to allow future cross-cutting concerns
/// (outbox pattern, retries, serialization, etc.) without leaking infrastructure details.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event asynchronously.
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="event">Event instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}