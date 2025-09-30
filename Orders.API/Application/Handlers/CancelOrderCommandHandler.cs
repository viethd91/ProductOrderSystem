using MediatR;
using Orders.API.Application.Commands;
using Orders.API.Domain.Enums;
using Orders.API.Domain.Interfaces;

namespace Orders.API.Application.Handlers;

/// <summary>
/// Command handler for cancelling orders
/// Implements CQRS pattern with business rule validation
/// </summary>
/// <param name="repository">Order repository for data persistence</param>
/// <param name="logger">Logger for diagnostics and monitoring</param>
public class CancelOrderCommandHandler(
    IOrderRepository repository,
    ILogger<CancelOrderCommandHandler> logger) : IRequestHandler<CancelOrderCommand, bool>
{
    /// <summary>
    /// Handles the cancel order command
    /// </summary>
    /// <param name="request">Cancel order command with cancellation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if order was successfully cancelled, false if it cannot be cancelled</returns>
    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation("Attempting to cancel order {OrderId}. Cancelled by: {CancelledBy}, Reason: {Reason}",
            request.OrderId, request.CancelledBy ?? "Unknown", request.Reason);

        try
        {
            // Validate command inputs
            ValidateCommand(request);

            // Retrieve existing order
            var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);
            if (order == null)
            {
                logger.LogWarning("Order {OrderId} not found for cancellation", request.OrderId);
                return false; // Order not found, cannot cancel
            }

            // Log current order state
            logger.LogInformation("Order {OrderId} ({OrderNumber}) current status: {CurrentStatus}, Customer: {CustomerName}",
                order.Id, order.OrderNumber, order.Status, order.CustomerName);

            // Check if order is already cancelled
            if (order.Status == OrderStatus.Cancelled)
            {
                logger.LogInformation("Order {OrderId} is already cancelled, no action needed", order.Id);
                return true; // Already cancelled, consider it successful
            }

            // Check if order can be cancelled based on current status
            if (!order.CanBeCancelled)
            {
                logger.LogWarning("Cannot cancel order {OrderId} - current status {CurrentStatus} does not allow cancellation. " +
                    "Only Pending and Confirmed orders can be cancelled.",
                    order.Id, order.Status);
                return false; // Cannot cancel order in current state
            }

            // Validate business rules for cancellation
            ValidateBusinessRules(order, request);

            // Cancel order using domain method
            var originalStatus = order.Status;
            try
            {
                order.Cancel();

                logger.LogInformation("Successfully cancelled order {OrderId} (was {OriginalStatus})",
                    order.Id, originalStatus);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError("Domain validation failed when cancelling order {OrderId}: {ErrorMessage}",
                    order.Id, ex.Message);
                return false; // Domain rules prevent cancellation
            }

            // Update in repository
            await repository.UpdateAsync(order, cancellationToken);

            // Save changes
            await repository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully persisted cancellation for order {OrderId}", order.Id);

            logger.LogInformation("Successfully cancelled order {OrderId} for customer {CustomerName}. " +
                "Original status: {OriginalStatus}, Total amount: {TotalAmount:C}",
                order.Id, order.CustomerName, originalStatus, order.TotalAmount);

            // Log additional context for monitoring and business intelligence
            LogCancellationContext(order, request, originalStatus);

            return true; // Successfully cancelled
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error cancelling order {OrderId}: {ErrorMessage}",
                request.OrderId, ex.Message);
            throw; // Re-throw unexpected errors
        }
    }

    /// <summary>
    /// Validates the cancel order command
    /// </summary>
    /// <param name="request">Cancel order command</param>
    private static void ValidateCommand(CancelOrderCommand request)
    {
        if (request.OrderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required", nameof(request.OrderId));
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Cancellation reason is required", nameof(request.Reason));
        }

        if (request.Reason.Length > 500)
        {
            throw new ArgumentException("Cancellation reason cannot exceed 500 characters", nameof(request.Reason));
        }

        // Validate optional fields if provided
        if (!string.IsNullOrEmpty(request.CancelledBy) && request.CancelledBy.Length > 200)
        {
            throw new ArgumentException("Cancelled by field cannot exceed 200 characters",
                nameof(request.CancelledBy));
        }

        if (!string.IsNullOrEmpty(request.CancellationDetails) && request.CancellationDetails.Length > 1000)
        {
            throw new ArgumentException("Cancellation details cannot exceed 1000 characters",
                nameof(request.CancellationDetails));
        }
    }

    /// <summary>
    /// Validates business rules specific to order cancellation
    /// </summary>
    /// <param name="order">Order being cancelled</param>
    /// <param name="request">Cancel command with cancellation context</param>
    private static void ValidateBusinessRules(Domain.Entities.Order order, CancelOrderCommand request)
    {
        // High-value order validation
        if (order.TotalAmount > 10000m && request.IsAutomaticCancellation)
        {
            throw new InvalidOperationException($"High-value orders (>{10000:C}) cannot be automatically cancelled. Manual review required.");
        }

        // Order age validation
        if (order.OrderDate < DateTime.UtcNow.AddDays(-30))
        {
            throw new InvalidOperationException("Orders older than 30 days cannot be cancelled through this process. Contact customer service.");
        }

        // Automatic cancellation refund validation
        if (request.IsAutomaticCancellation && request.ProcessRefundImmediately)
        {
            throw new InvalidOperationException("Automatic cancellations cannot process refunds immediately. Manual review is required.");
        }
    }

    /// <summary>
    /// Logs cancellation context for monitoring and business intelligence
    /// </summary>
    /// <param name="order">Cancelled order</param>
    /// <param name="request">Cancel command with cancellation context</param>
    /// <param name="originalStatus">Original order status before cancellation</param>
    private void LogCancellationContext(Domain.Entities.Order order, CancelOrderCommand request, OrderStatus originalStatus)
    {
        logger.LogInformation("Order Cancellation Metrics - OrderId: {OrderId}, OriginalStatus: {OriginalStatus}, " +
            "TotalAmount: {TotalAmount:C}, ItemCount: {ItemCount}, OrderAge: {OrderAge}, " +
            "IsAutomatic: {IsAutomatic}, ProcessRefund: {ProcessRefund}",
            order.Id, originalStatus, order.TotalAmount, order.OrderItems.Count,
            DateTime.UtcNow - order.OrderDate, request.IsAutomaticCancellation, request.ProcessRefundImmediately);

        logger.LogDebug("Cancellation Details for order {OrderId}: {CancellationDetails}",
            order.Id, request.CancellationDetails ?? "No additional details provided");

        if (!request.NotifyCustomer)
            logger.LogInformation("Customer notification suppressed for cancelled order {OrderId}", order.Id);

        LogCancellationScenario(order, request, originalStatus);
    }

    /// <summary>
    /// Logs specific cancellation scenario for analytics
    /// </summary>
    /// <param name="order">Cancelled order</param>
    /// <param name="request">Cancel command</param>
    /// <param name="originalStatus">Original order status</param>
    private void LogCancellationScenario(Domain.Entities.Order order, CancelOrderCommand request, OrderStatus originalStatus)
    {
        var scenario = (originalStatus, request.IsAutomaticCancellation, order.TotalAmount) switch
        {
            (OrderStatus.Pending, true, _) => "Automatic Pending Cancellation",
            (OrderStatus.Pending, false, var amount) when amount > 1000m => "High-Value Pending Manual Cancellation",
            (OrderStatus.Pending, false, _) => "Standard Pending Manual Cancellation",
            (OrderStatus.Confirmed, true, _) => "Automatic Confirmed Cancellation",
            (OrderStatus.Confirmed, false, _) => "Manual Confirmed Cancellation",
            _ => "Other Cancellation Scenario"
        };

        logger.LogInformation("Cancellation Scenario: {Scenario} for order {OrderId}", scenario, order.Id);

        var orderAge = DateTime.UtcNow - order.OrderDate;
        var cancellationTiming = orderAge.TotalHours switch
        {
            < 1 => "Immediate",
            < 24 => "Same Day",
            < 168 => "Same Week",
            _ => "Late"
        };

        logger.LogDebug("Cancellation Timing: {Timing} ({OrderAge:c}) for order {OrderId}",
            cancellationTiming, orderAge, order.Id);
    }
}