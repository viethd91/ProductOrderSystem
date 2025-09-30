using Orders.API.Domain.Enums;

namespace Orders.API.Domain.Events;

/// <summary>
/// Domain event raised when an order is cancelled
/// </summary>
/// <param name="OrderId">The unique identifier of the cancelled order</param>
/// <param name="OrderNumber">The order number</param>
/// <param name="Reason">The reason for cancellation</param>
public record OrderCancelledEvent(
    Guid OrderId,
    string OrderNumber,
    string Reason
) : DomainEvent
{
    /// <summary>
    /// Additional timestamp for audit purposes
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Event type identifier for logging and debugging
    /// </summary>
    public string EventType => nameof(OrderCancelledEvent);

    /// <summary>
    /// Customer who placed the order (for notification purposes)
    /// </summary>
    public Guid? CustomerId { get; init; }

    /// <summary>
    /// Customer name (for notification purposes)
    /// </summary>
    public string? CustomerName { get; init; }

    /// <summary>
    /// Total amount of the cancelled order (for refund processing)
    /// </summary>
    public decimal? TotalAmount { get; init; }

    /// <summary>
    /// The status the order was in before cancellation
    /// </summary>
    public OrderStatus? PreviousStatus { get; init; }

    /// <summary>
    /// User or system that initiated the cancellation
    /// </summary>
    public string? CancelledBy { get; init; }

    /// <summary>
    /// Additional details about the cancellation
    /// </summary>
    public string? CancellationDetails { get; init; }

    /// <summary>
    /// Whether to process refund immediately (if applicable)
    /// </summary>
    public bool ProcessRefundImmediately { get; init; }

    /// <summary>
    /// Indicates if this was an automatic cancellation (system-initiated)
    /// </summary>
    public bool IsAutomaticCancellation { get; init; }

    /// <summary>
    /// Gets a human-readable description of the cancellation
    /// </summary>
    public string CancellationDescription => 
        $"Order {OrderNumber} was cancelled. Reason: {Reason}";
}