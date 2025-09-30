using MediatR;
using Orders.API.Application.DTOs;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Enums;

namespace Orders.API.Application.Queries;

/// <summary>
/// Query for retrieving orders by their status
/// Implements CQRS pattern with MediatR for read-only operations
/// </summary>
/// <param name="Status">The order status to filter by</param>
public record GetOrdersByStatusQuery(
    OrderStatus Status
) : IRequest<List<OrderDto>>
{
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
    /// Minimum total amount filter - optional
    /// If provided, only orders with total amount >= this value will be returned
    /// </summary>
    public decimal? MinAmount { get; init; }

    /// <summary>
    /// Maximum total amount filter - optional
    /// If provided, only orders with total amount <= this value will be returned
    /// </summary>
    public decimal? MaxAmount { get; init; }

    /// <summary>
    /// User requesting the orders (for audit/logging purposes)
    /// </summary>
    public string? RequestedBy { get; init; }
}