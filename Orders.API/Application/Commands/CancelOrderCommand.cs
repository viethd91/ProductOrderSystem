using MediatR;

namespace Orders.API.Application.Commands;

/// <summary>
/// Command for cancelling an order
/// Follows CQRS pattern with MediatR
/// </summary>
/// <param name="OrderId">Order unique identifier to cancel</param>
/// <param name="Reason">Reason for cancellation (required for audit)</param>
public record CancelOrderCommand(
    Guid OrderId,
    string Reason
) : IRequest<bool>
{
    /// <summary>
    /// User or system performing the cancellation
    /// </summary>
    public string? CancelledBy { get; init; }

    /// <summary>
    /// Whether this is an automatic cancellation (system-initiated)
    /// </summary>
    public bool IsAutomaticCancellation { get; init; } = false;

    /// <summary>
    /// Customer notification preference for cancellation
    /// </summary>
    public bool NotifyCustomer { get; init; } = true;

    /// <summary>
    /// Additional details about the cancellation
    /// </summary>
    public string? CancellationDetails { get; init; }

    /// <summary>
    /// Whether to process refund immediately (if applicable)
    /// </summary>
    public bool ProcessRefundImmediately { get; init; } = false;
}