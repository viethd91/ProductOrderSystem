namespace Shared.Messaging;

/// <summary>
/// Provides a single shared in-memory bus instance so both Products.API and Orders.API
/// publish/subscribe against the same in-process dispatcher (demo-only).
/// </summary>
public static class MessageBusConfiguration
{
    private static IMessageBus? _sharedInstance;
    private static readonly object _lock = new();

    public static IMessageBus GetSharedInstance()
    {
        if (_sharedInstance == null)
        {
            lock (_lock)
            {
                _sharedInstance ??= new InMemoryMessageBus(); // Logger optional
            }
        }
        return _sharedInstance;
    }
}