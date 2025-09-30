namespace Orders.API.Application.DTOs;

/// <summary>
/// DTO for customer order statistics
/// Used for analytics and customer insights
/// </summary>
public record CustomerOrderStatsDto
{
    /// <summary>
    /// Customer identifier
    /// </summary>
    public Guid CustomerId { get; init; }

    /// <summary>
    /// Total number of orders
    /// </summary>
    public int TotalOrders { get; init; }

    /// <summary>
    /// Total amount across all orders
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Average order amount
    /// </summary>
    public decimal AverageOrderAmount { get; init; }

    /// <summary>
    /// Number of completed orders (Delivered status)
    /// </summary>
    public int CompletedOrders { get; init; }

    /// <summary>
    /// Number of cancelled orders
    /// </summary>
    public int CancelledOrders { get; init; }

    /// <summary>
    /// Number of pending orders
    /// </summary>
    public int PendingOrders { get; init; }

    /// <summary>
    /// Date of the first order
    /// </summary>
    public DateTime? FirstOrderDate { get; init; }

    /// <summary>
    /// Date of the most recent order
    /// </summary>
    public DateTime? LastOrderDate { get; init; }

    /// <summary>
    /// Customer loyalty score based on order history
    /// </summary>
    public decimal LoyaltyScore => TotalOrders > 0 ? (decimal)CompletedOrders / TotalOrders * 100 : 0;

    /// <summary>
    /// Indicates if the customer is a high-value customer
    /// </summary>
    public bool IsHighValueCustomer => TotalAmount > 5000m;

    /// <summary>
    /// Customer status based on order activity
    /// </summary>
    public string CustomerStatus => (TotalOrders, LastOrderDate) switch
    {
        (0, _) => "New",
        (_, var lastOrder) when lastOrder?.AddDays(90) < DateTime.UtcNow => "Inactive",
        (var orders, _) when orders >= 10 => "VIP",
        (var orders, _) when orders >= 5 => "Regular",
        _ => "Casual"
    };
}