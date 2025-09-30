namespace Orders.API.Domain.Enums;

/// <summary>
/// Represents the status of an order in the system
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order has been created but not yet confirmed
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Order has been confirmed and is being processed
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Order has been shipped to the customer
    /// </summary>
    Shipped = 2,

    /// <summary>
    /// Order has been delivered to the customer
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// Order has been cancelled
    /// </summary>
    Cancelled = 4
}