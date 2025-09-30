using MediatR;
using Orders.API.Application.DTOs;

namespace Orders.API.Application.Queries;

/// <summary>
/// Query for retrieving all orders for a specific customer
/// Implements CQRS pattern with MediatR for read-only operations
/// </summary>
/// <param name="CustomerId">The customer identifier to filter by</param>
public record GetOrdersByCustomerIdQuery(
    Guid CustomerId
) : IRequest<List<OrderDto>>
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
    /// Sort by field (CreatedAt, OrderDate, TotalAmount)
    /// Default is "CreatedAt"
    /// </summary>
    public string SortBy { get; init; } = "CreatedAt";

    /// <summary>
    /// Sort direction (asc, desc)
    /// Default is descending (newest first)
    /// </summary>
    public string SortDirection { get; init; } = "desc";

    /// <summary>
    /// Start date filter (inclusive) - optional
    /// If provided, only orders from this date onwards will be returned
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// End date filter (inclusive) - optional
    /// If provided, only orders up to this date will be returned
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// User requesting the orders (for audit/logging purposes)
    /// </summary>
    public string? RequestedBy { get; init; }
}