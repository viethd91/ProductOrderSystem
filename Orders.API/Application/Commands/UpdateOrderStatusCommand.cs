using MediatR;
using Orders.API.Application.DTOs;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Enums;

namespace Orders.API.Application.Commands;

/// <summary>
/// Command for updating an order's status
/// Follows CQRS pattern with MediatR and enforces business rules
/// </summary>
/// <param name="OrderId">Order unique identifier</param>
/// <param name="NewStatus">New status to transition to</param>
public record UpdateOrderStatusCommand(
    Guid OrderId,
    OrderStatus NewStatus
) : IRequest<OrderDto>
{
    /// <summary>
    /// Reason for status change (for audit and customer communication)
    /// </summary>
    public string? StatusChangeReason { get; init; }

    /// <summary>
    /// User or system performing the status update
    /// </summary>
    public string? UpdatedBy { get; init; }

    /// <summary>
    /// Additional notes related to the status change
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Tracking number (relevant for Shipped status)
    /// </summary>
    public string? TrackingNumber { get; init; }

    /// <summary>
    /// Estimated delivery date (relevant for Shipped status)
    /// </summary>
    public DateTime? EstimatedDeliveryDate { get; init; }
}