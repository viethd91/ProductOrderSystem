using MediatR;
using Microsoft.Extensions.Logging;
using Orders.API.Application.IntegrationEvents;
using Orders.API.Domain.Interfaces;
using Shared.IntegrationEvents;

namespace Orders.API.Application.IntegrationEvents.Handlers;

public class ProductPriceChangedEventHandler : IIntegrationEventHandler<ProductPriceChangedIntegrationEvent>
{
    private readonly IOrderRepository _orders;
    private readonly ILogger<ProductPriceChangedEventHandler> _logger;

    public ProductPriceChangedEventHandler(IOrderRepository orders, ILogger<ProductPriceChangedEventHandler> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    public async Task HandleAsync(ProductPriceChangedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogInformation(
            "Handling ProductPriceChangedIntegrationEvent: ProductId={ProductId}, Name={ProductName}, OldPrice={OldPrice}, NewPrice={NewPrice}, Change={Delta} ({Percent:F2}%), Increase={IsIncrease}",
            @event.ProductId,
            @event.ProductName,
            @event.OldPrice,
            @event.NewPrice,
            @event.PriceChange,
            @event.PercentageChange,
            @event.IsIncrease);

        // Query pending orders containing this product (reactive projection update)
        var pendingOrders = await _orders.GetPendingOrdersByProductIdAsync(@event.ProductId, cancellationToken);

        if (pendingOrders.Count == 0)
        {
            _logger.LogInformation(
                "No pending orders affected by product {ProductId}", @event.ProductId);
            return;
        }

        foreach (var order in pendingOrders)
        {
            // Update order items with new price (if business rules allow)
            foreach (var item in order.OrderItems.Where(i => i.ProductId == @event.ProductId))
            {
                var oldItemPrice = item.UnitPrice;
                item.UpdateUnitPrice(@event.NewPrice); // Assuming this method exists

                _logger.LogInformation("Updated order item price from {OldPrice:C} to {NewPrice:C} in order {OrderId}",
                    oldItemPrice, @event.NewPrice, order.Id);
            }

            order.RecalculateTotal(); // Recalculate order total
            await _orders.UpdateAsync(order, cancellationToken);
        }

        await _orders.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated totals for {Count} pending orders due to product price change", pendingOrders.Count);
    }
}