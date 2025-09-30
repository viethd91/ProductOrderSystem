namespace Products.API.Domain.Events;

/// <summary>
/// Domain event raised when a new product is created
/// </summary>
/// <param name="ProductId">The unique identifier of the created product</param>
/// <param name="ProductName">The name of the created product</param>
/// <param name="Price">The price of the created product</param>
public record ProductCreatedEvent(
    Guid ProductId,
    string ProductName,
    decimal Price
) : DomainEvent
{
    /// <summary>
    /// Additional timestamp for audit purposes
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    
    /// <summary>
    /// Event type identifier for logging and debugging
    /// </summary>
    public string EventType => nameof(ProductCreatedEvent);
}