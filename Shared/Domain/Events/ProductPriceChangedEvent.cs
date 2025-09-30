namespace Shared.Domain.Events;

/// <summary>
/// Domain event raised when a product's price changes
/// This event enables cross-service communication between Products and Orders APIs
/// </summary>
/// <param name="ProductId">The unique identifier of the product</param>
/// <param name="ProductName">The name of the product</param>
/// <param name="OldPrice">The previous price</param>
/// <param name="NewPrice">The new price</param>
public record ProductPriceChangedEvent(
    Guid ProductId,
    string ProductName,
    decimal OldPrice,
    decimal NewPrice
)
{
    /// <summary>
    /// Unique event identifier
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the price change occurred
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the event occurred (alternative naming)
    /// </summary>
    public DateTime OccurredOn => Timestamp;

    /// <summary>
    /// Event type identifier for logging and debugging
    /// </summary>
    public string EventType => nameof(ProductPriceChangedEvent);

    /// <summary>
    /// Price change amount (positive for increase, negative for decrease)
    /// </summary>
    public decimal PriceChange => NewPrice - OldPrice;

    /// <summary>
    /// Percentage change in price
    /// </summary>
    public decimal PercentageChange => OldPrice == 0 ? 0 : (PriceChange / OldPrice) * 100;

    /// <summary>
    /// Indicates if this was a price increase
    /// </summary>
    public bool IsPriceIncrease => NewPrice > OldPrice;

    /// <summary>
    /// Indicates if this was a price decrease
    /// </summary>
    public bool IsPriceDecrease => NewPrice < OldPrice;

    /// <summary>
    /// Human-readable description of the price change
    /// </summary>
    public string PriceChangeDescription => 
        $"Product '{ProductName}' price changed from {OldPrice:C} to {NewPrice:C} ({PriceChange:C})";
}