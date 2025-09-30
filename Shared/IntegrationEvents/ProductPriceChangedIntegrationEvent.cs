namespace Shared.IntegrationEvents;

/// <summary>
/// Integration event published when a product's price changes (cross-microservice contract)
/// </summary>
public sealed record ProductPriceChangedIntegrationEvent(
    Guid ProductId,
    string ProductName,
    decimal OldPrice,
    decimal NewPrice,
    DateTime ChangedAtUtc = default)
{
    public DateTime ChangedAtUtc { get; init; } = ChangedAtUtc == default ? DateTime.UtcNow : ChangedAtUtc;

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
    public bool IsIncrease => NewPrice > OldPrice;
}