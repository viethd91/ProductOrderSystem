using MediatR;
using Orders.API.Application.DTOs;

namespace Orders.API.Application.Queries;

/// <summary>
/// Query for retrieving all orders
/// Implements CQRS pattern with MediatR for read-only operations
/// </summary>
public record GetAllOrdersQuery : IRequest<List<OrderDto>>
{
    /// <summary>
    /// Whether to include cancelled orders
    /// Default is false (exclude cancelled orders due to global query filter)
    /// </summary>
    public bool IncludeCancelled { get; init; } = false;

    /// <summary>
    /// Maximum number of orders to return
    /// Default is 0 (no limit), useful for performance control
    /// </summary>
    public int MaxResults { get; init; } = 0;

    /// <summary>
    /// Sort by field (CreatedAt, OrderDate, TotalAmount, CustomerName)
    /// Default is "CreatedAt"
    /// </summary>
    public string SortBy { get; init; } = "CreatedAt";

    /// <summary>
    /// Sort direction (asc, desc)
    /// Default is descending (newest first)
    /// </summary>
    public string SortDirection { get; init; } = "desc";

    /// <summary>
    /// User requesting the orders (for audit/logging purposes)
    /// </summary>
    public string? RequestedBy { get; init; }
}