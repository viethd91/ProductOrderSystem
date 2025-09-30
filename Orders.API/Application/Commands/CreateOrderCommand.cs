using MediatR;
using Orders.API.Application.DTOs;

namespace Orders.API.Application.Commands;

/// <summary>
/// Command for creating a new order
/// Follows CQRS pattern with MediatR
/// </summary>
/// <param name="CustomerId">Customer identifier placing the order</param>
/// <param name="CustomerName">Customer name (required, max 200 chars)</param>
/// <param name="OrderItems">List of items to include in the order</param>
public record CreateOrderCommand(
    Guid CustomerId,
    string CustomerName,
    List<CreateOrderItemDto> OrderItems
) : IRequest<OrderDto>
{
    /// <summary>
    /// Optional special instructions for the order
    /// </summary>
    public string? SpecialInstructions { get; init; }

    /// <summary>
    /// User or system creating the order (for audit purposes)
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Expected delivery date (optional)
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; init; }
}