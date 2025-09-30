using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.Messaging;

/// <summary>
/// Thread-safe in-memory message bus for demo/dev scenarios.
/// Not for production (no persistence / retries / ordering guarantees).
/// </summary>
public sealed class InMemoryMessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<Type, ConcurrentBag<Func<object, Task>>> _handlers = new();
    private readonly ILogger<InMemoryMessageBus> _logger;

    public InMemoryMessageBus(ILogger<InMemoryMessageBus>? logger = null)
    {
        _logger = logger ?? NullLogger<InMemoryMessageBus>.Instance;
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        
        var messageType = typeof(T);
        var wrappedHandler = new Func<object, Task>(obj => handler((T)obj));
        
        _handlers.AddOrUpdate(
            messageType,
            _ => new ConcurrentBag<Func<object, Task>> { wrappedHandler },
            (_, existing) =>
            {
                existing.Add(wrappedHandler);
                return existing;
            });
            
        _logger.LogDebug("Handler subscribed for message type {MessageType}. Total handlers: {HandlerCount}", 
            messageType.Name, _handlers[messageType].Count);
    }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = typeof(T);
        
        if (_handlers.TryGetValue(messageType, out var handlerBag))
        {
            var handlers = handlerBag.ToArray(); // Create snapshot for thread safety
            
            _logger.LogDebug("Publishing to {HandlerCount} handlers for message type {MessageType}",
                handlers.Length, messageType.Name);

            if (handlers.Length == 0)
            {
                _logger.LogTrace("No handlers found in bag for message type {MessageType}", messageType.Name);
                return Task.CompletedTask;
            }

            var tasks = handlers.Select(h => SafeInvoke(h, message, cancellationToken));
            return Task.WhenAll(tasks);
        }

        _logger.LogTrace("No handlers registered for message type {MessageType}", messageType.Name);
        return Task.CompletedTask;
    }

    private async Task SafeInvoke(Func<object, Task> handler, object message, CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested) return;
            await handler(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message type {MessageType}", message.GetType().Name);
        }
    }
}