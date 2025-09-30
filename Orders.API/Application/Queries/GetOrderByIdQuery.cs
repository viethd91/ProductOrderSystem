using MediatR;
using Orders.API.Application.DTOs;

namespace Orders.API.Application.Queries;

/// <summary>
/// Query for retrieving a single order by its unique identifier
/// Implements CQRS pattern with MediatR for read-only operations
/// </summary>
/// <param name="Id">The order unique identifier</param>
public record GetOrderByIdQuery(
    Guid Id
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