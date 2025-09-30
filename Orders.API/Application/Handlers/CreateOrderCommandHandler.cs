using MediatR;
using Orders.API.Application.Commands;
using Orders.API.Application.DTOs;
using Orders.API.Application.Extensions;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Interfaces;
using Shared.Messaging;
using Shared.IntegrationEvents;

namespace Orders.API.Application.Handlers;

/// <summary>
/// Command handler for creating new orders
/// Implements CQRS pattern with domain event publishing and integration event publishing
/// </summary>
/// <param name="repository">Order repository for data persistence</param>
/// <param name="messageBus">Message bus for publishing domain events</param>
/// <param name="logger">Logger for diagnostics and monitoring</param>
public class CreateOrderCommandHandler(
    IOrderRepository repository,
    IMessageBus messageBus,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateOrderCommand, OrderDto>
{
    /// <summary>
    /// Handles the create order command
    /// </summary>
    /// <param name="request">Create order command with order details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OrderDto representing the created order</returns>
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation("Creating new order for customer {CustomerId} ({CustomerName}) with {ItemCount} items",
            request.CustomerId, request.CustomerName, request.OrderItems?.Count ?? 0);

        try
        {
            // Validate that we have order items
            if (request.OrderItems == null || !request.OrderItems.Any())
            {
                throw new ArgumentException("Order must contain at least one item");
            }

            // Create order entity using domain constructor (order number is auto-generated)
            var order = new Order(request.CustomerId, request.CustomerName);

            // Add order items
            foreach (var itemDto in request.OrderItems)
            {
                var orderItem = new OrderItem(
                    itemDto.ProductId,
                    itemDto.ProductName,
                    itemDto.Quantity,
                    itemDto.UnitPrice);

                order.AddOrderItem(orderItem);
            }

            // Persist order
            await repository.AddAsync(order, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully created order {OrderId} ({OrderNumber}) for customer {CustomerName}",
                order.Id, order.OrderNumber, order.CustomerName);

            // Publish domain events
            await PublishDomainEvents(order, cancellationToken);

            // Publish integration event
            await PublishOrderCreatedIntegrationEvent(order, cancellationToken);

            // Map to DTO and return
            var orderDto = order.ToDto();

            logger.LogInformation("Order creation completed for order {OrderId} with total amount {TotalAmount:C}",
                order.Id, order.TotalAmount);

            return orderDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create order for customer {CustomerId}: {ErrorMessage}",
                request.CustomerId, ex.Message);
            throw;
        }
    }

    private async Task PublishDomainEvents(Order order, CancellationToken cancellationToken)
    {
        var domainEvents = order.DomainEvents.ToList();

        if (!domainEvents.Any())
        {
            logger.LogDebug("No domain events to publish for order {OrderId}", order.Id);
            return;
        }

        logger.LogDebug("Publishing {EventCount} domain event(s) for order {OrderId}",
            domainEvents.Count, order.Id);

        foreach (var domainEvent in domainEvents)
        {
            try
            {
                await messageBus.PublishAsync(domainEvent, cancellationToken);
                logger.LogDebug("Successfully published domain event: {EventType} for order {OrderId}",
                    domainEvent.GetType().Name, order.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish domain event: {EventType} for order {OrderId}",
                    domainEvent.GetType().Name, order.Id);
                throw;
            }
        }

        order.ClearDomainEvents();
    }

    private async Task PublishOrderCreatedIntegrationEvent(Order order, CancellationToken cancellationToken)
    {
        try
        {
            var integrationEvent = new OrderCreatedIntegrationEvent(
                order.Id,
                order.CustomerId,
                order.TotalAmount,
                order.OrderItems.Select(item => new OrderCreatedIntegrationEvent.OrderItemDto(
                    item.ProductId,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice)).ToList()
            );

            await messageBus.PublishAsync(integrationEvent, cancellationToken);
            logger.LogInformation("Published OrderCreatedIntegrationEvent for order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish integration event for order {OrderId}", order.Id);
            // Don't throw here as the order was successfully created
        }
    }
}