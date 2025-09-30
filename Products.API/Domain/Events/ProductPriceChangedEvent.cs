namespace Products.API.Domain.Events;

/// <summary>
/// Domain event raised when a product's price changes
/// </summary>
/// <param name="ProductId">The unique identifier of the product</param>
/// <param name="OldPrice">The previous price</param>
/// <param name="NewPrice">The new price</param>
public record ProductPriceChangedEvent(
    Guid ProductId,
    decimal OldPrice,
    decimal NewPrice
) : DomainEvent
{
    /// <summary>
    /// Additional timestamp for audit purposes
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    
    /// <summary>
    /// Event type identifier for logging and debugging
    /// </summary>
    public string EventType => nameof(ProductPriceChangedEvent);
    
    /// <summary>
    /// Calculate the price change amount
    /// </summary>
    public decimal PriceChange => NewPrice - OldPrice;
    
    /// <summary>
    /// Calculate the percentage change in price
    /// </summary>
    public decimal PercentageChange => 
        OldPrice == 0 ? 0 : Math.Round((PriceChange / OldPrice) * 100, 2);
    
    /// <summary>
    /// Indicates if this is a price increase
    /// </summary>
    public bool IsPriceIncrease => NewPrice > OldPrice;
}