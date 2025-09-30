namespace Shared.IntegrationEvents;

public sealed record ProductPriceChangedIntegrationEvent(
    Guid ProductId,
    string ProductName,
    decimal OldPrice,
    decimal NewPrice,
    DateTime ChangedAtUtc = default)
{
    public DateTime ChangedAtUtc { get; init; } = ChangedAtUtc == default ? DateTime.UtcNow : ChangedAtUtc;
    
    public decimal PriceChange => NewPrice - OldPrice;
    public decimal PercentageChange => OldPrice == 0 ? 0 : (PriceChange / OldPrice) * 100;
    public bool IsIncrease => NewPrice > OldPrice;
}