using Microsoft.Extensions.Hosting;
using Orders.API.Application.IntegrationEvents;
using Shared.Messaging; // Use shared interface
using Shared.IntegrationEvents;

namespace Orders.API.Infrastructure.Messaging;

/// <summary>
/// Background service that subscribes to integration events from the message bus
/// </summary>
public class EventBusSubscriptionService : BackgroundService
{
    private readonly IMessageBus _bus; // Now uses Shared.Messaging.IMessageBus
    private readonly IServiceProvider _services;
    private readonly ILogger<EventBusSubscriptionService> _logger;

    public EventBusSubscriptionService(IMessageBus bus, IServiceProvider services, ILogger<EventBusSubscriptionService> logger)
    {
        _bus = bus;
        _services = services;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting event bus subscriptions (Orders.API)");
        _logger.LogInformation("Message bus type: {BusType}", _bus.GetType().Name);

        _bus.Subscribe<ProductPriceChangedIntegrationEvent>(e => Dispatch(e, stoppingToken));
        _logger.LogInformation("Subscribed to ProductPriceChangedIntegrationEvent");
        
        _bus.Subscribe<ProductDeletedIntegrationEvent>(e => Dispatch(e, stoppingToken));
        _logger.LogInformation("Subscribed to ProductDeletedIntegrationEvent");
        
        _bus.Subscribe<ProductCreatedIntegrationEvent>(e => Dispatch(e, stoppingToken));
        _logger.LogInformation("Subscribed to ProductCreatedIntegrationEvent");

        _logger.LogInformation("All event bus subscriptions completed");
        return Task.CompletedTask;
    }

    private async Task Dispatch<T>(T evt, CancellationToken ct) where T : class
    {
        using var scope = _services.CreateScope();
        var handlerType = typeof(IIntegrationEventHandler<T>);
        var handlers = scope.ServiceProvider.GetServices(handlerType).Cast<IIntegrationEventHandler<T>>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(evt, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling integration event {EventType}", typeof(T).Name);
            }
        }
    }
}