namespace Orders.API.Application.DTOs;

/// <summary>
/// Lightweight summary DTO for Order entity
/// Used for list views and performance-critical scenarios
/// </summary>
public record OrderSummaryDto
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
    /// Number of items in the order
    /// </summary>
    public required int ItemCount { get; init; }

    /// <summary>
    /// Formatted total amount for display
    /// </summary>
    public string FormattedTotalAmount { get; init; } = string.Empty;

    /// <summary>
    /// Order age from creation
    /// </summary>
    public TimeSpan OrderAge { get; init; }
}