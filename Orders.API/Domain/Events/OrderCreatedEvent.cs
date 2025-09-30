namespace Orders.API.Domain.Events;

/// <summary>
/// Domain event raised when a new order is created
/// </summary>
/// <param name="OrderId">The unique identifier of the created order</param>
/// <param name="OrderNumber">The order number</param>
/// <param name="CustomerId">The customer who placed the order</param>
/// <param name="CustomerName">The customer name</param>
public record OrderCreatedEvent(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName
) : DomainEvent
{
    /// <summary>
    /// Total amount of the order
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// List of items in the order
    /// </summary>
    public List<OrderItemDto> OrderItems { get; init; } = [];

    /// <summary>
    /// Additional timestamp for audit purposes
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Event type identifier for logging and debugging
    /// </summary>
    public string EventType => nameof(OrderCreatedEvent);

    /// <summary>
    /// Order item data transfer object for event payload
    /// </summary>
    /// <param name="ProductId">Product identifier</param>
    /// <param name="ProductName">Product name at time of order</param>
    /// <param name="Quantity">Quantity ordered</param>
    /// <param name="UnitPrice">Unit price at time of order</param>
    public record OrderItemDto(
        Guid ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPrice
    )
    {
        /// <summary>
        /// Total price for this line item
        /// </summary>
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}