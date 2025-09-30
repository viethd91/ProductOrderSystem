using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Shared.Messaging; // Use shared interface

namespace Orders.API.Infrastructure.Messaging;

/// <summary>
/// In-memory implementation of IMessageBus using ConcurrentDictionary
/// Suitable for single-instance applications and development/testing scenarios
/// Registered as Singleton to maintain handlers across the application lifetime
/// 
/// Note: This is a demo implementation for development and testing purposes.
/// In production environments, consider using distributed message brokers like:
/// - RabbitMQ with MassTransit
/// - Azure Service Bus
/// - Apache Kafka
/// - AWS SQS/SNS
/// </summary>
public class InMemoryMessageBus : IMessageBus // Now implements Shared.Messaging.IMessageBus
{
    private readonly ConcurrentDictionary<Type, ConcurrentBag<Delegate>> _handlers = new();
    private readonly ILogger<InMemoryMessageBus> _logger;

    /// <summary>
    /// Constructor with logger dependency injection
    /// </summary>
    /// <param name="logger">Logger for diagnostic information</param>
    public InMemoryMessageBus(ILogger<InMemoryMessageBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Constructor without logger (for shared instance creation)
    /// </summary>
    public InMemoryMessageBus()
    {
        _logger = null!; // Will be null for shared instances
    }

    /// <summary>
    /// Publishes a message asynchronously to all registered handlers
    /// Executes all handlers in parallel for better performance
    /// </summary>
    /// <typeparam name="T">Type of message to publish</typeparam>
    /// <param name="message">The message instance to publish</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Task representing the async operation</returns>
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = typeof(T);
        var messageName = messageType.Name;

        _logger?.LogDebug("Publishing message {MessageType} with content: {@Message}", messageName, message);

        if (!_handlers.TryGetValue(messageType, out var handlerBag) || handlerBag.IsEmpty)
        {
            _logger?.LogDebug("No handlers registered for message type {MessageType}", messageName);
            return;
        }

        var handlers = handlerBag.ToArray();
        _logger?.LogDebug("Found {HandlerCount} handler(s) for message type {MessageType}", handlers.Length, messageName);

        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            if (handler is Func<T, Task> typedHandler)
            {
                var task = ExecuteHandlerSafely(typedHandler, message, messageName, cancellationToken);
                tasks.Add(task);
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
            _logger?.LogDebug("Successfully executed {TaskCount} handler(s) for message {MessageType}", tasks.Count, messageName);
        }
    }

    /// <summary>
    /// Subscribes a handler function to messages of type T
    /// Multiple handlers can be registered for the same message type
    /// </summary>
    /// <typeparam name="T">Type of message to subscribe to</typeparam>
    /// <param name="handler">Handler function that will process the message</param>
    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var messageType = typeof(T);
        var messageName = messageType.Name;

        _handlers.AddOrUpdate(
            messageType,
            _ =>
            {
                _logger?.LogDebug("Creating new handler collection for message type {MessageType}", messageName);
                return new ConcurrentBag<Delegate> { handler };
            },
            (_, existingBag) =>
            {
                existingBag.Add(handler);
                _logger?.LogDebug("Added handler to existing collection for message type {MessageType}. Total handlers: {HandlerCount}", 
                    messageName, existingBag.Count);
                return existingBag;
            });

        _logger?.LogInformation("Successfully subscribed handler for message type {MessageType}", messageName);
    }

    /// <summary>
    /// Unsubscribes a specific handler from messages of type T
    /// Note: Due to ConcurrentBag limitations, this creates a new bag without the specified handler
    /// </summary>
    /// <typeparam name="T">Type of message to unsubscribe from</typeparam>
    /// <param name="handler">Handler function to remove</param>
    public void Unsubscribe<T>(Func<T, Task> handler) where T : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var messageType = typeof(T);
        var messageName = messageType.Name;

        if (!_handlers.TryGetValue(messageType, out var handlerBag))
        {
            _logger?.LogDebug("No handlers found for message type {MessageType} during unsubscribe", messageName);
            return;
        }

        // Create new bag without the specified handler
        var remainingHandlers = handlerBag.Where(h => !ReferenceEquals(h, handler)).ToArray();
        
        if (remainingHandlers.Length == 0)
        {
            _handlers.TryRemove(messageType, out _);
            _logger?.LogDebug("Removed all handlers for message type {MessageType}", messageName);
        }
        else
        {
            _handlers[messageType] = new ConcurrentBag<Delegate>(remainingHandlers);
            _logger?.LogDebug("Unsubscribed handler for message type {MessageType}. Remaining handlers: {HandlerCount}", 
                messageName, remainingHandlers.Length);
        }
    }

    /// <summary>
    /// Executes a message handler safely with error handling and logging
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    /// <param name="handler">Handler function to execute</param>
    /// <param name="message">Message to process</param>
    /// <param name="messageName">Message type name for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the handler execution</returns>
    private async Task ExecuteHandlerSafely<T>(
        Func<T, Task> handler, 
        T message, 
        string messageName, 
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            _logger?.LogDebug("Executing handler for message type {MessageType}", messageName);
            await handler(message);
            _logger?.LogDebug("Successfully executed handler for message type {MessageType}", messageName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning("Handler execution was cancelled for message type {MessageType}", messageName);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing handler for message type {MessageType}: {ErrorMessage}", 
                messageName, ex.Message);
        }
    }
}