using MediatR;
using Orders.API.Application.DTOs;

namespace Orders.API.Application.Queries;

/// <summary>
/// Query for retrieving orders within a specific date range
/// Useful for reporting and analytics scenarios
/// </summary>
/// <param name="StartDate">Start date (inclusive)</param>
/// <param name="EndDate">End date (inclusive)</param>
public record GetOrdersByDateRangeQuery(
    DateTime StartDate,
    DateTime EndDate
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
    /// Sort by field (OrderDate, CreatedAt, TotalAmount, CustomerName)
    /// Default is "OrderDate"
    /// </summary>
    public string SortBy { get; init; } = "OrderDate";

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