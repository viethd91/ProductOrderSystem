using static Shared.IntegrationEvents.OrderCreatedIntegrationEvent;

namespace Shared.IntegrationEvents;

/// <summary>
/// Integration event published by Orders service without leaking internal domain entities.
/// </summary>
public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    IReadOnlyCollection<OrderItemDto> Items,
    DateTime CreatedAtUtc = default)
{
    public DateTime CreatedAtUtc { get; init; } = CreatedAtUtc == default ? DateTime.UtcNow : CreatedAtUtc;

    public sealed record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
}