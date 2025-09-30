using Microsoft.Extensions.Logging;
using Orders.API.Application.IntegrationEvents;
using Orders.API.Domain.Interfaces;
using Shared.IntegrationEvents;

namespace Orders.API.Application.IntegrationEvents.Handlers;

/// <summary>
/// Handles product price change integration events from the Products service
/// Updates pending orders with new product prices
/// </summary>
public class ProductPriceChangedEventHandler : IIntegrationEventHandler<ProductPriceChangedIntegrationEvent>
{
    private readonly IOrderRepository _orders;
    private readonly ILogger<ProductPriceChangedEventHandler> _logger;

    public ProductPriceChangedEventHandler(IOrderRepository orders, ILogger<ProductPriceChangedEventHandler> logger)
    {
        _orders = orders ?? throw new ArgumentNullException(nameof(orders));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        try
        {
            // Query pending orders containing this product (reactive projection update)
            var pendingOrders = await _orders.GetPendingOrdersByProductIdAsync(@event.ProductId, cancellationToken);

            if (pendingOrders.Count == 0)
            {
                _logger.LogInformation(
                    "No pending orders affected by product {ProductId}", @event.ProductId);
                return;
            }

            _logger.LogInformation(
                "Found {Count} pending orders to update for product {ProductId}",
                pendingOrders.Count, @event.ProductId);

            var updatedOrdersCount = 0;
            var updatedItemsCount = 0;

            foreach (var order in pendingOrders)
            {
                var orderHasUpdates = false;

                // Update order items with new price (if business rules allow)
                foreach (var item in order.OrderItems.Where(i => i.ProductId == @event.ProductId))
                {
                    var oldItemPrice = item.UnitPrice;

                    // Business rule: Only update prices for pending orders
                    if (order.CanBeModified)
                    {
                        item.UpdateUnitPrice(@event.NewPrice);
                        orderHasUpdates = true;
                        updatedItemsCount++;

                        _logger.LogInformation(
                            "Updated order item price from {OldPrice:C} to {NewPrice:C} in order {OrderId} (Item: {ItemId})",
                            oldItemPrice, @event.NewPrice, order.Id, item.Id);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Skipped price update for order {OrderId} item {ItemId} - order cannot be modified (Status: {Status})",
                            order.Id, item.Id, order.Status);
                    }
                }

                if (orderHasUpdates)
                {
                    // Recalculate order total
                    order.RecalculateTotal();

                    // Update the order in repository
                    await _orders.UpdateAsync(order, cancellationToken);
                    updatedOrdersCount++;

                    _logger.LogInformation(
                        "Recalculated total for order {OrderId}, new total: {NewTotal:C}",
                        order.Id, order.TotalAmount);
                }
            }

            // Save all changes at once
            if (updatedOrdersCount > 0)
            {
                await _orders.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully updated {OrdersCount} orders and {ItemsCount} order items due to product price change for product {ProductId}",
                    updatedOrdersCount, updatedItemsCount, @event.ProductId);
            }
            else
            {
                _logger.LogInformation(
                    "No orders were updated for product {ProductId} - all affected orders are in non-modifiable status",
                    @event.ProductId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling ProductPriceChangedIntegrationEvent for product {ProductId}: {ErrorMessage}",
                @event.ProductId, ex.Message);
            throw;
        }
    }
}