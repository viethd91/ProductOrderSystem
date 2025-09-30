using MediatR;
using Orders.API.Application.DTOs;

namespace Orders.API.Application.Queries;

/// <summary>
/// Query for retrieving orders that require attention
/// Useful for operational monitoring and customer service scenarios
/// Returns orders that are pending for longer than specified days
/// </summary>
public record GetOrdersRequiringAttentionQuery : IRequest<List<OrderDto>>
{
    /// <summary>
    /// Number of days to consider as requiring attention
    /// Default is 3 days
    /// </summary>
    public int DaysOld { get; init; } = 3;

    /// <summary>
    /// Maximum number of orders to return
    /// Default is 0 (no limit), useful for performance control
    /// </summary>
    public int MaxResults { get; init; } = 0;

    /// <summary>
    /// User requesting the orders (for audit/logging purposes)
    /// </summary>
    public string? RequestedBy { get; init; }
}