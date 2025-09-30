using MediatR;
using Orders.API.Application.Commands;
using Orders.API.Application.DTOs;
using Orders.API.Application.Extensions;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Enums;
using Orders.API.Domain.Interfaces;
using Shared.Messaging; // Use shared interface

namespace Orders.API.Application.Handlers;

/// <summary>
/// Command handler for updating order status
/// Implements CQRS pattern with domain event publishing and status transition validation
/// </summary>
/// <param name="repository">Order repository for data persistence</param>
/// <param name="messageBus">Message bus for publishing domain events</param>
/// <param name="logger">Logger for diagnostics and monitoring</param>
public class UpdateOrderStatusCommandHandler(
    IOrderRepository repository,
    IMessageBus messageBus, // Now uses Shared.Messaging.IMessageBus
    ILogger<UpdateOrderStatusCommandHandler> logger) : IRequestHandler<UpdateOrderStatusCommand, OrderDto>
{
    /// <summary>
    /// Handles the update order status command
    /// </summary>
    /// <param name="request">Update order status command with order details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OrderDto representing the updated order</returns>
    public async Task<OrderDto> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation("Updating status for order {OrderId} to {NewStatus}. Updated by: {UpdatedBy}",
            request.OrderId, request.NewStatus, request.UpdatedBy ?? "Unknown");

        try
        {
            // Validate command inputs
            ValidateCommand(request);

            // Retrieve existing order
            var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                var message = $"Order with ID {request.OrderId} not found";
                logger.LogWarning(message);
                throw new InvalidOperationException(message);
            }

            // Log current order state
            var oldStatus = order.Status;
            logger.LogInformation("Order {OrderId} ({OrderNumber}) current status: {CurrentStatus}, target status: {NewStatus}",
                order.Id, order.OrderNumber, oldStatus, request.NewStatus);

            // Check if status change is actually needed
            if (oldStatus == request.NewStatus)
            {
                logger.LogInformation("Order {OrderId} is already in status {Status}, no update needed",
                    order.Id, request.NewStatus);
                return order.ToDto();
            }

            // Validate business rules for status transition
            if (order.IsFinalState)
            {
                throw new InvalidOperationException(
                    $"Cannot update status of order {order.Id} because it is in final state {oldStatus}");
            }

            // Update status using domain method (this validates status transition and raises domain events)
            try
            {
                order.UpdateStatus(request.NewStatus);
                
                logger.LogInformation("Successfully updated order {OrderId} status from {OldStatus} to {NewStatus}",
                    order.Id, oldStatus, request.NewStatus);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError("Status transition validation failed for order {OrderId}: {ErrorMessage}",
                    order.Id, ex.Message);
                throw; // Re-throw domain validation error
            }

            // Add any additional context from the command to domain events
            AddContextualInformation(order, request);

            // Update in repository
            await repository.UpdateAsync(order, cancellationToken);

            // Save changes (this triggers domain event collection in OrderContext)
            await repository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully persisted status change for order {OrderId}", order.Id);

            // Publish domain events collected from the entity
            await PublishDomainEvents(order, cancellationToken);

            // Map domain entity to DTO using extension method
            var orderDto = order.ToDto();

            logger.LogInformation("Successfully processed status update for order {OrderId} from {OldStatus} to {NewStatus}",
                order.Id, oldStatus, request.NewStatus);

            // Log additional context for specific status changes
            LogStatusSpecificInformation(order, oldStatus, request);

            return orderDto;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError("Failed to update order status for order {OrderId}: {ErrorMessage}",
                request.OrderId, ex.Message);
            throw; // Re-throw business rule violations
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error updating order status for order {OrderId}: {ErrorMessage}",
                request.OrderId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Validates the update order status command
    /// </summary>
    /// <param name="request">Update order status command</param>
    private static void ValidateCommand(UpdateOrderStatusCommand request)
    {
        if (request.OrderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required", nameof(request.OrderId));
        }

        if (!Enum.IsDefined<OrderStatus>(request.NewStatus))
        {
            throw new ArgumentException($"Invalid order status: {request.NewStatus}", nameof(request.NewStatus));
        }

        // Validate optional fields if provided
        if (!string.IsNullOrEmpty(request.StatusChangeReason) && request.StatusChangeReason.Length > 500)
        {
            throw new ArgumentException("Status change reason cannot exceed 500 characters", 
                nameof(request.StatusChangeReason));
        }

        if (!string.IsNullOrEmpty(request.TrackingNumber) && request.TrackingNumber.Length > 100)
        {
            throw new ArgumentException("Tracking number cannot exceed 100 characters", 
                nameof(request.TrackingNumber));
        }

        if (!string.IsNullOrEmpty(request.UpdatedBy) && request.UpdatedBy.Length > 200)
        {
            throw new ArgumentException("Updated by field cannot exceed 200 characters", 
                nameof(request.UpdatedBy));
        }
    }

    /// <summary>
    /// Adds contextual information from the command to the order for audit purposes
    /// This information could be stored in additional audit tables or logged
    /// </summary>
    /// <param name="order">Order entity being updated</param>
    /// <param name="request">Update command with contextual information</param>
    private void AddContextualInformation(Order order, UpdateOrderStatusCommand request)
    {
        // In a real implementation, you might:
        // 1. Store audit information in a separate audit table
        // 2. Add the information to domain events
        // 3. Update order properties if the domain supports it

        logger.LogDebug("Adding contextual information for order {OrderId} status update", order.Id);

        if (!string.IsNullOrEmpty(request.StatusChangeReason))
        {
            logger.LogDebug("Status change reason for order {OrderId}: {Reason}", 
                order.Id, request.StatusChangeReason);
        }

        if (!string.IsNullOrEmpty(request.TrackingNumber))
        {
            logger.LogDebug("Tracking number for order {OrderId}: {TrackingNumber}", 
                order.Id, request.TrackingNumber);
        }

        if (request.EstimatedDeliveryDate.HasValue)
        {
            logger.LogDebug("Estimated delivery date for order {OrderId}: {EstimatedDeliveryDate}", 
                order.Id, request.EstimatedDeliveryDate.Value);
        }

        if (!string.IsNullOrEmpty(request.Notes))
        {
            logger.LogDebug("Additional notes for order {OrderId}: {Notes}", 
                order.Id, request.Notes);
        }
    }

    /// <summary>
    /// Logs status-specific information and business implications
    /// </summary>
    /// <param name="order">Updated order</param>
    /// <param name="oldStatus">Previous status</param>
    /// <param name="request">Update command</param>
    private void LogStatusSpecificInformation(Order order, OrderStatus oldStatus, UpdateOrderStatusCommand request)
    {
        switch (request.NewStatus)
        {
            case OrderStatus.Confirmed:
                logger.LogInformation("Order {OrderId} confirmed. Total amount: {TotalAmount:C}, Items: {ItemCount}",
                    order.Id, order.TotalAmount, order.OrderItems.Count);
                break;

            case OrderStatus.Shipped:
                logger.LogInformation("Order {OrderId} shipped. Tracking number: {TrackingNumber}, " +
                    "Estimated delivery: {EstimatedDeliveryDate}",
                    order.Id, 
                    request.TrackingNumber ?? "Not provided",
                    request.EstimatedDeliveryDate?.ToString("yyyy-MM-dd") ?? "Not provided");
                break;

            case OrderStatus.Delivered:
                logger.LogInformation("Order {OrderId} delivered successfully. Customer: {CustomerName}",
                    order.Id, order.CustomerName);
                break;

            case OrderStatus.Cancelled:
                logger.LogWarning("Order {OrderId} cancelled. Previous status: {OldStatus}, Reason: {Reason}",
                    order.Id, oldStatus, request.StatusChangeReason ?? "No reason provided");
                break;
        }
    }

    /// <summary>
    /// Publishes all domain events from the order entity
    /// </summary>
    /// <param name="order">Order entity with domain events</param>
    /// <param name="cancellationToken">Cancellation token</param>
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
                logger.LogDebug("Publishing domain event: {EventType} for order {OrderId}",
                    domainEvent.GetType().Name, order.Id);

                await messageBus.PublishAsync(domainEvent, cancellationToken);

                logger.LogDebug("Successfully published domain event: {EventType} for order {OrderId}",
                    domainEvent.GetType().Name, order.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish domain event: {EventType} for order {OrderId}",
                    domainEvent.GetType().Name, order.Id);
                throw; // Re-throw to ensure transaction rollback
            }
        }

        // Clear events after publishing (important to prevent re-publishing)
        order.ClearDomainEvents();

        logger.LogInformation("Successfully published all domain events for order {OrderId}", order.Id);
    }

    /// <summary>
    /// Validates business rules specific to status transitions
    /// This method contains additional business logic beyond domain validation
    /// </summary>
    /// <param name="order">Order being updated</param>
    /// <param name="newStatus">Target status</param>
    /// <param name="request">Update command with additional context</param>
    private static void ValidateBusinessRules(Order order, OrderStatus newStatus, UpdateOrderStatusCommand request)
    {
        // Additional business validation beyond domain rules
        switch (newStatus)
        {
            case OrderStatus.Shipped:
                // Require tracking number for shipped orders
                if (string.IsNullOrWhiteSpace(request.TrackingNumber))
                {
                    throw new InvalidOperationException("Tracking number is required when shipping an order");
                }
                break;

            case OrderStatus.Delivered:
                // Ensure order was previously shipped
                if (order.Status != OrderStatus.Shipped)
                {
                    throw new InvalidOperationException("Order must be shipped before it can be delivered");
                }
                break;

            case OrderStatus.Cancelled:
                // Validate cancellation reason for audit purposes
                if (string.IsNullOrWhiteSpace(request.StatusChangeReason))
                {
                    throw new InvalidOperationException("Cancellation reason is required when cancelling an order");
                }
                break;
        }
    }
}