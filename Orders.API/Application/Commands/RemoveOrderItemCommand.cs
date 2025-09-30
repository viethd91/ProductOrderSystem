using MediatR;
using Orders.API.Application.DTOs;

namespace Orders.API.Application.Commands;

/// <summary>
/// Command for removing an item from an existing order
/// Can only be performed on orders in Pending status
/// </summary>
/// <param name="OrderId">Order unique identifier to remove item from</param>
/// <param name="OrderItemId">Order item unique identifier to remove</param>
public record RemoveOrderItemCommand(
    Guid OrderId,
    Guid OrderItemId
) : IRequest<OrderDto>
{
    /// <summary>
    /// User or system removing the item
    /// </summary>
    public string? RemovedBy { get; init; }

    /// <summary>
    /// Reason for removing the item (for audit purposes)
    /// </summary>
    public string? RemovalReason { get; init; }

    /// <summary>
    /// Whether to notify customer of item removal
    /// </summary>
    public bool NotifyCustomer { get; init; } = true;
}