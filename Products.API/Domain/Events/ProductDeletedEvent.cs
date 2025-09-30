namespace Products.API.Domain.Events;

/// <summary>
/// Domain event raised when a product is deleted
/// </summary>
/// <param name="ProductId">The unique identifier of the deleted product</param>
/// <param name="ProductName">The name of the deleted product</param>
public record ProductDeletedEvent(
    Guid ProductId,
    string ProductName
) : DomainEvent
{
    /// <summary>
    /// Additional timestamp for audit purposes
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    
    /// <summary>
    /// Event type identifier for logging and debugging
    /// </summary>
    public string EventType => nameof(ProductDeletedEvent);
    
    /// <summary>
    /// Reason for deletion (optional)
    /// </summary>
    public string? DeletionReason { get; init; }
    
    /// <summary>
    /// User or system that initiated the deletion
    /// </summary>
    public string? DeletedBy { get; init; }
}