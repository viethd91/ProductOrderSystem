using Orders.API.Domain.Entities;
using Orders.API.Domain.Enums;

namespace Orders.API.Domain.Events;

/// <summary>
/// Domain event raised when an order's status changes
/// </summary>
/// <param name="OrderId">The unique identifier of the order</param>
/// <param name="OrderNumber">The order number</param>
/// <param name="OldStatus">The previous status</param>
/// <param name="NewStatus">The new status</param>
public record OrderStatusChangedEvent(
    Guid OrderId,
    string OrderNumber,
    OrderStatus OldStatus,
    OrderStatus NewStatus
) : DomainEvent
{
    /// <summary>
    /// Additional timestamp for audit purposes
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Event type identifier for logging and debugging
    /// </summary>
    public string EventType => nameof(OrderStatusChangedEvent);

    /// <summary>
    /// Indicates if this is a status progression (forward movement)
    /// </summary>
    public bool IsStatusProgression => IsForwardTransition(OldStatus, NewStatus);

    /// <summary>
    /// Indicates if this is a cancellation
    /// </summary>
    public bool IsCancellation => NewStatus == OrderStatus.Cancelled;

    /// <summary>
    /// Indicates if the order reached a final state
    /// </summary>
    public bool IsOrderCompleted => NewStatus is OrderStatus.Delivered or OrderStatus.Cancelled;

    /// <summary>
    /// Gets a human-readable description of the status change
    /// </summary>
    public string StatusChangeDescription => $"Order {OrderNumber} status changed from {OldStatus} to {NewStatus}";

    /// <summary>
    /// Determines if the status change represents forward progress
    /// </summary>
    private static bool IsForwardTransition(OrderStatus oldStatus, OrderStatus newStatus)
    {
        return oldStatus switch
        {
            OrderStatus.Pending when newStatus == OrderStatus.Confirmed => true,
            OrderStatus.Confirmed when newStatus == OrderStatus.Shipped => true,
            OrderStatus.Shipped when newStatus == OrderStatus.Delivered => true,
            _ => false
        };
    }
}