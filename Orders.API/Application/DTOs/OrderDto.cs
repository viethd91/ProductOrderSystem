using Orders.API.Domain.Entities;
using Orders.API.Domain.Enums;

namespace Orders.API.Application.DTOs;

/// <summary>
/// Data Transfer Object for Order entity
/// Used for API responses and cross-boundary communication
/// </summary>
public record OrderDto
{
    /// <summary>
    /// Unique identifier for the order
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Unique order number in format ORD-{timestamp}
    /// </summary>
    public string OrderNumber { get; init; } = string.Empty;

    /// <summary>
    /// Customer identifier who placed the order
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Customer name for display purposes
    /// </summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>
    /// Date and time when the order was placed
    /// </summary>
    public DateTime OrderDate { get; init; }

    /// <summary>
    /// Current status of the order (string representation)
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Total amount calculated from all order items
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// List of items in this order
    /// </summary>
    public List<OrderItemDto> OrderItems { get; init; } = [];

    /// <summary>
    /// Date and time when the order was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Date and time when the order was last updated
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Number of items in the order (calculated property)
    /// </summary>
    public int ItemCount => OrderItems?.Count ?? 0;

    /// <summary>
    /// Indicates if the order can be modified (Pending status)
    /// </summary>
    public bool CanBeModified => Status.Equals(nameof(OrderStatus.Pending), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Indicates if the order is in a final state
    /// </summary>
    public bool IsFinalState => Status.Equals(nameof(OrderStatus.Delivered), StringComparison.OrdinalIgnoreCase) ||
                                Status.Equals(nameof(OrderStatus.Cancelled), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Indicates if the order can be cancelled
    /// </summary>
    public bool CanBeCancelled => Status.Equals(nameof(OrderStatus.Pending), StringComparison.OrdinalIgnoreCase) ||
                                  Status.Equals(nameof(OrderStatus.Confirmed), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Order status as enum value for internal use
    /// </summary>
    public OrderStatus StatusEnum => Enum.TryParse<OrderStatus>(Status, out var result) ? result : OrderStatus.Pending;

    /// <summary>
    /// Human-readable status description
    /// </summary>
    public string StatusDescription => StatusEnum switch
    {
        OrderStatus.Pending => "Order is pending processing",
        OrderStatus.Confirmed => "Order has been confirmed and is being prepared",
        OrderStatus.Shipped => "Order has been shipped for delivery",
        OrderStatus.Delivered => "Order has been delivered successfully",
        OrderStatus.Cancelled => "Order has been cancelled",
        _ => "Unknown status"
    };

    /// <summary>
    /// Average item price (total amount / item count)
    /// </summary>
    public decimal AverageItemPrice => ItemCount > 0 ? TotalAmount / ItemCount : 0m;

    /// <summary>
    /// Order summary for display purposes
    /// </summary>
    public string OrderSummary => $"{OrderNumber} - {CustomerName} - {Status} - {TotalAmount:C} ({ItemCount} items)";
}