using MediatR;
using Orders.API.Application.DTOs;

namespace Orders.API.Application.Queries;

/// <summary>
/// Query for retrieving the most recent orders
/// Useful for dashboard and monitoring scenarios
/// </summary>
public record GetRecentOrdersQuery : IRequest<List<OrderDto>>
{
    /// <summary>
    /// Number of recent orders to retrieve
    /// Default is 10, maximum is 100
    /// </summary>
    public int Count { get; init; } = 10;

    /// <summary>
    /// Whether to include cancelled orders
    /// Default is false (exclude cancelled orders due to global query filter)
    /// </summary>
    public bool IncludeCancelled { get; init; } = false;

    /// <summary>
    /// User requesting the orders (for audit/logging purposes)
    /// </summary>
    public string? RequestedBy { get; init; }
}