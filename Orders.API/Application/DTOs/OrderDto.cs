using Orders.API.Domain.Entities;
using Orders.API.Domain.Enums;

namespace Orders.API.Application.DTOs;

/// <summary>
/// Data Transfer Object for Order entity
/// Used for API responses and CQRS query results
/// </summary>
public record OrderDto
{
    /// <summary>
    /// Order unique identifier
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Unique order number
    /// </summary>
    public required string OrderNumber { get; init; }

    /// <summary>
    /// Customer identifier
    /// </summary>
    public required Guid CustomerId { get; init; }

    /// <summary>
    /// Customer name
    /// </summary>
    public required string CustomerName { get; init; }

    /// <summary>
    /// Date and time when the order was placed
    /// </summary>
    public required DateTime OrderDate { get; init; }

    /// <summary>
    /// Current order status
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Total amount of the order
    /// </summary>
    public required decimal TotalAmount { get; init; }

    /// <summary>
    /// Collection of order items
    /// </summary>
    public required List<OrderItemDto> OrderItems { get; init; }

    /// <summary>
    /// Date and time when the order was created
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Date and time when the order was last updated
    /// </summary>
    public required DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Number of items in the order
    /// </summary>
    public int ItemCount { get; init; }

    /// <summary>
    /// Whether the order can be modified
    /// </summary>
    public bool IsModifiable { get; init; }

    /// <summary>
    /// Whether the order is in a final state
    /// </summary>
    public bool IsFinalState { get; init; }

    /// <summary>
    /// Whether the order can be cancelled
    /// </summary>
    public bool CanBeCancelled { get; init; }

    /// <summary>
    /// Formatted total amount for display
    /// </summary>
    public string FormattedTotalAmount => TotalAmount.ToString("C");

    /// <summary>
    /// Order age from creation
    /// </summary>
    public TimeSpan OrderAge => DateTime.UtcNow - OrderDate;
}