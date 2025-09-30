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
    private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _handlers = new();
    private readonly ILogger<InMemoryMessageBus> _logger;

    public InMemoryMessageBus(ILogger<InMemoryMessageBus>? logger = null)
    {
        _logger = logger ?? NullLogger<InMemoryMessageBus>.Instance;
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        var list = _handlers.GetOrAdd(typeof(T), _ => new List<Func<object, Task>>());
        lock (list)
        {
            list.Add(o => handler((T)o));
        }
        _logger.LogDebug("Handler subscribed for message type {MessageType}", typeof(T).Name);
    }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_handlers.TryGetValue(typeof(T), out var list))
        {
            List<Func<object, Task>> snapshot;
            lock (list)
            {
                snapshot = list.ToList();
            }

            _logger.LogDebug("Publishing {HandlerCount} handlers for message type {MessageType}",
                snapshot.Count, typeof(T).Name);

            var tasks = snapshot.Select(h => SafeInvoke(h, message, cancellationToken));
            return Task.WhenAll(tasks);
        }

        _logger.LogTrace("No handlers registered for message type {MessageType}", typeof(T).Name);
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