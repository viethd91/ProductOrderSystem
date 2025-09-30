namespace Shared.IntegrationEvents;

/// <summary>
/// Integration event published when a product is created (cross-microservice contract).
/// </summary>
public sealed record ProductCreatedIntegrationEvent(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Stock,
    DateTime CreatedAtUtc = default)
{
    public DateTime CreatedAtUtc { get; init; } = CreatedAtUtc == default ? DateTime.UtcNow : CreatedAtUtc;
}