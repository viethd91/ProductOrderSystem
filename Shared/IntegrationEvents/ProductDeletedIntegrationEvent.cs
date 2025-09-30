namespace Shared.IntegrationEvents;

public sealed record ProductDeletedIntegrationEvent(
    Guid ProductId,
    string ProductName,
    bool HardDelete,
    string? Reason,
    DateTime DeletedAtUtc = default)
{
    public DateTime DeletedAtUtc { get; init; } = DeletedAtUtc == default ? DateTime.UtcNow : DeletedAtUtc;
}