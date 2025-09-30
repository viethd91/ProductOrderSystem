using MediatR;
using Orders.API.Application.DTOs;
using Orders.API.Application.Extensions;
using Orders.API.Application.Queries;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Enums;
using Orders.API.Domain.Interfaces;

namespace Orders.API.Application.Handlers;

/// <summary>
/// Query handler for retrieving orders by customer ID
/// Implements CQRS pattern with MediatR for read-only operations
/// </summary>
/// <param name="repository">Order repository for data access</param>
/// <param name="logger">Logger for diagnostics and monitoring</param>
public class GetOrdersByCustomerIdQueryHandler(
    IOrderRepository repository,
    ILogger<GetOrdersByCustomerIdQueryHandler> logger) : IRequestHandler<GetOrdersByCustomerIdQuery, List<OrderDto>>
{
    /// <summary>
    /// Handles the get orders by customer ID query
    /// </summary>
    /// <param name="request">Query request with customer ID and filtering parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of OrderDto for the specified customer</returns>
    public async Task<List<OrderDto>> Handle(GetOrdersByCustomerIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogDebug("Retrieving orders for customer {CustomerId}. Requested by: {RequestedBy}, " +
            "Include cancelled: {IncludeCancelled}, Max results: {MaxResults}, " +
            "Date range: {StartDate} to {EndDate}, Sort by: {SortBy} {SortDirection}",
            request.CustomerId, request.RequestedBy ?? "Unknown", request.IncludeCancelled,
            request.MaxResults, request.StartDate, request.EndDate, request.SortBy, request.SortDirection);

        try
        {
            // Validate customer ID
            if (request.CustomerId == Guid.Empty)
            {
                logger.LogWarning("Invalid customer ID provided: {CustomerId}", request.CustomerId);
                return [];
            }

            // Get orders for customer from repository (includes OrderItems)
            var orders = await repository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

            logger.LogDebug("Found {OrderCount} orders for customer {CustomerId}", 
                orders.Count, request.CustomerId);

            // Filter out cancelled orders if not explicitly requested
            if (!request.IncludeCancelled)
            {
                orders = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
                logger.LogDebug("Filtered out cancelled orders. Remaining orders: {OrderCount}", orders.Count);
            }

            // Apply date range filtering
            if (request.StartDate.HasValue || request.EndDate.HasValue)
            {
                orders = ApplyDateRangeFilter(orders, request.StartDate, request.EndDate);
                logger.LogDebug("Applied date range filter. Remaining orders: {OrderCount}", orders.Count);
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

            logger.LogInformation("Successfully retrieved {OrderCount} orders for customer {CustomerId}",
                orderDtos.Count, request.CustomerId);

            return orderDtos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving orders for customer {CustomerId}: {ErrorMessage}",
                request.CustomerId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Applies date range filtering to the orders list
    /// </summary>
    /// <param name="orders">List of orders to filter</param>
    /// <param name="startDate">Start date filter (inclusive)</param>
    /// <param name="endDate">End date filter (inclusive)</param>
    /// <returns>Filtered list of orders</returns>
    private static List<Order> ApplyDateRangeFilter(List<Order> orders, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue)
        {
            orders = orders.Where(o => o.OrderDate >= startDate.Value).ToList();
        }

        if (endDate.HasValue)
        {
            // Include the full end date (up to 23:59:59.999)
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            orders = orders.Where(o => o.OrderDate <= endOfDay).ToList();
        }

        return orders;
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

            "ordernumber" => isDescending
                ? orders.OrderByDescending(o => o.OrderNumber).ToList()
                : orders.OrderBy(o => o.OrderNumber).ToList(),

            _ => orders.OrderByDescending(o => o.CreatedAt).ToList() // Default sort by CreatedAt descending
        };
    }
}