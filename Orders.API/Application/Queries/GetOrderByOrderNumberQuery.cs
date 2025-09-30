using MediatR;
using Orders.API.Application.DTOs;

namespace Orders.API.Application.Queries;

/// <summary>
/// Query for retrieving a single order by its order number
/// Useful for customer service and order tracking scenarios
/// </summary>
/// <param name="OrderNumber">The order number to search for</param>
public record GetOrderByOrderNumberQuery(
    string OrderNumber
) : IRequest<OrderDto?>
{
    /// <summary>
    /// Whether to include cancelled orders
    /// Default is false (exclude cancelled orders due to global query filter)
    /// </summary>
    public bool IncludeCancelled { get; init; } = false;

    /// <summary>
    /// User requesting the order (for audit/logging purposes)
    /// </summary>
    public string? RequestedBy { get; init; }
}