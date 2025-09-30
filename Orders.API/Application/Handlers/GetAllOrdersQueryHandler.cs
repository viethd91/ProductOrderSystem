using MediatR;
using Orders.API.Application.DTOs;
using Orders.API.Application.Extensions;
using Orders.API.Application.Queries;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Orders.API.Domain.Enums;

namespace Orders.API.Application.Handlers;

/// <summary>
/// Query handler for retrieving all orders
/// Implements CQRS pattern with MediatR for read-only operations
/// </summary>
/// <param name="repository">Order repository for data access</param>
/// <param name="logger">Logger for diagnostics and monitoring</param>
public class GetAllOrdersQueryHandler(
    IOrderRepository repository,
    ILogger<GetAllOrdersQueryHandler> logger) : IRequestHandler<GetAllOrdersQuery, List<OrderDto>>
{
    /// <summary>
    /// Handles the get all orders query
    /// </summary>
    /// <param name="request">Query request with optional filtering parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of OrderDto representing all orders</returns>
    public async Task<List<OrderDto>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogDebug("Retrieving all orders. Requested by: {RequestedBy}, Include cancelled: {IncludeCancelled}, " +
            "Max results: {MaxResults}, Sort by: {SortBy} {SortDirection}",
            request.RequestedBy ?? "Unknown", request.IncludeCancelled, 
            request.MaxResults, request.SortBy, request.SortDirection);

        try
        {
            // Get all orders from repository (includes OrderItems and applies global query filter)
            var orders = await repository.GetAllAsync(cancellationToken);

            // Filter out cancelled orders if not explicitly requested
            if (!request.IncludeCancelled)
            {
                orders = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
                logger.LogDebug("Filtered out cancelled orders. Remaining orders: {OrderCount}", orders.Count);
            }

            // Apply sorting if specified
            if (!string.IsNullOrEmpty(request.SortBy))
            {
                orders = ApplySorting(orders, request.SortBy, request.SortDirection);
                logger.LogDebug("Applied sorting by {SortBy} {SortDirection}", request.SortBy, request.SortDirection);
            }

            // Apply max results limit
            if (request.MaxResults > 0 && orders.Count > request.MaxResults)
            {
                orders = orders.Take(request.MaxResults).ToList();
                logger.LogDebug("Limited results to {MaxResults} orders", request.MaxResults);
            }

            // Map domain entities to DTOs using extension method
            var orderDtos = orders.Select(o => o.ToDto()).ToList();

            logger.LogInformation("Successfully retrieved {OrderCount} orders", orderDtos.Count);

            return orderDtos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all orders: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Applies sorting to the orders list based on specified criteria
    /// </summary>
    /// <param name="orders">List of orders to sort</param>
    /// <param name="sortBy">Field to sort by</param>
    /// <param name="sortDirection">Sort direction (asc/desc)</param>
    /// <returns>Sorted list of orders</returns>
    private static List<Order> ApplySorting(List<Order> orders, string sortBy, string sortDirection)
    {
        var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLower() switch
        {
            "ordernumber" => isDescending
                ? orders.OrderByDescending(o => o.OrderNumber).ToList()
                : orders.OrderBy(o => o.OrderNumber).ToList(),

            "customername" => isDescending
                ? orders.OrderByDescending(o => o.CustomerName).ToList()
                : orders.OrderBy(o => o.CustomerName).ToList(),

            "orderdate" => isDescending
                ? orders.OrderByDescending(o => o.OrderDate).ToList()
                : orders.OrderBy(o => o.OrderDate).ToList(),

            "totalamount" => isDescending
                ? orders.OrderByDescending(o => o.TotalAmount).ToList()
                : orders.OrderBy(o => o.TotalAmount).ToList(),

            "status" => isDescending
                ? orders.OrderByDescending(o => o.Status).ToList()
                : orders.OrderBy(o => o.Status).ToList(),

            "createdat" => isDescending
                ? orders.OrderByDescending(o => o.CreatedAt).ToList()
                : orders.OrderBy(o => o.CreatedAt).ToList(),

            "updatedat" => isDescending
                ? orders.OrderByDescending(o => o.UpdatedAt).ToList()
                : orders.OrderBy(o => o.UpdatedAt).ToList(),

            _ => orders.OrderByDescending(o => o.CreatedAt).ToList() // Default sort by CreatedAt descending
        };
    }
}