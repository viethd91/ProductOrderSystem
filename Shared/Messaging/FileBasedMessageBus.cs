using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Shared.Messaging;

public class FileBasedMessageBus : IMessageBus
{
    private readonly string _messageDirectory;
    private readonly ILogger<FileBasedMessageBus> _logger;
    private readonly Dictionary<Type, List<Func<object, Task>>> _handlers = new();
    private readonly Timer _pollingTimer;

    public FileBasedMessageBus(ILogger<FileBasedMessageBus> logger)
    {
        _logger = logger;
        _messageDirectory = Path.Combine(Path.GetTempPath(), "MessageBus");
        Directory.CreateDirectory(_messageDirectory);
        
        // Poll for new messages every 100ms
        _pollingTimer = new Timer(ProcessMessages, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        var messageType = typeof(T);
        if (!_handlers.ContainsKey(messageType))
        {
            _handlers[messageType] = new List<Func<object, Task>>();
        }
        _handlers[messageType].Add(obj => handler((T)obj));
        
        _logger.LogInformation("Subscribed to {MessageType}", messageType.Name);
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var messageType = typeof(T).Name;
        var fileName = $"{messageType}_{DateTimeOffset.UtcNow.Ticks}_{Guid.NewGuid()}.json";
        var filePath = Path.Combine(_messageDirectory, fileName);
        
        var json = JsonSerializer.Serialize(message);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        
        _logger.LogInformation("Published {MessageType} to file {FileName}", messageType, fileName);
    }

    private async void ProcessMessages(object? state)
    {
        try
        {
            var files = Directory.GetFiles(_messageDirectory, "*.json");
            
            foreach (var file in files)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var messageTypeName = fileName.Split('_')[0];
                    
                    // Find matching handler
                    var handlerType = _handlers.Keys.FirstOrDefault(t => t.Name == messageTypeName);
                    if (handlerType != null)
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var message = JsonSerializer.Deserialize(json, handlerType);
                        
                        if (message != null && _handlers.TryGetValue(handlerType, out var handlers))
                        {
                            var tasks = handlers.Select(h => h(message));
                            await Task.WhenAll(tasks);
                        }
                    }
                    
                    // Delete processed file
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message file {File}", file);
                    // Move to error folder or delete
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in message processing timer");
        }
    }
}