namespace Orders.API.Application.DTOs;

/// <summary>
/// Lightweight Order summary DTO for list views and dashboards
/// Contains essential order information without detailed order items
/// </summary>
public record OrderSummaryDto
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
    /// Number of items in the order
    /// </summary>
    public int ItemCount { get; init; }

    /// <summary>
    /// Date and time when the order was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Date and time when the order was last updated
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Order summary line for display
    /// </summary>
    public string Summary => $"{OrderNumber} - {CustomerName} - {Status} - {TotalAmount:C}";
}