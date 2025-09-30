using MediatR;
using Orders.API.Application.DTOs;
using Orders.API.Application.Extensions;
using Orders.API.Application.Queries;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Interfaces;

namespace Orders.API.Application.Handlers;

/// <summary>
/// Query handler for retrieving orders by status
/// Implements CQRS pattern with MediatR for read-only operations
/// </summary>
/// <param name="repository">Order repository for data access</param>
/// <param name="logger">Logger for diagnostics and monitoring</param>
public class GetOrdersByStatusQueryHandler(
    IOrderRepository repository,
    ILogger<GetOrdersByStatusQueryHandler> logger) : IRequestHandler<GetOrdersByStatusQuery, List<OrderDto>>
{
    /// <summary>
    /// Handles the get orders by status query
    /// </summary>
    /// <param name="request">Query request with status and filtering parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of OrderDto with the specified status</returns>
    public async Task<List<OrderDto>> Handle(GetOrdersByStatusQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogDebug("Retrieving orders with status {Status}. Requested by: {RequestedBy}, " +
            "Max results: {MaxResults}, Date range: {StartDate} to {EndDate}, " +
            "Amount range: {MinAmount} to {MaxAmount}, Sort by: {SortBy} {SortDirection}",
            request.Status, request.RequestedBy ?? "Unknown", request.MaxResults,
            request.StartDate, request.EndDate, request.MinAmount, request.MaxAmount, 
            request.SortBy, request.SortDirection);

        try
        {
            // Get orders by status from repository (includes OrderItems)
            var orders = await repository.GetByStatusAsync(request.Status, cancellationToken);

            logger.LogDebug("Found {OrderCount} orders with status {Status}", 
                orders.Count, request.Status);

            // Apply date range filtering
            if (request.StartDate.HasValue || request.EndDate.HasValue)
            {
                orders = ApplyDateRangeFilter(orders, request.StartDate, request.EndDate);
                logger.LogDebug("Applied date range filter. Remaining orders: {OrderCount}", orders.Count);
            }

            // Apply amount range filtering
            if (request.MinAmount.HasValue || request.MaxAmount.HasValue)
            {
                orders = ApplyAmountRangeFilter(orders, request.MinAmount, request.MaxAmount);
                logger.LogDebug("Applied amount range filter. Remaining orders: {OrderCount}", orders.Count);
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

            logger.LogInformation("Successfully retrieved {OrderCount} orders with status {Status}",
                orderDtos.Count, request.Status);

            return orderDtos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving orders with status {Status}: {ErrorMessage}",
                request.Status, ex.Message);
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
    /// Applies amount range filtering to the orders list
    /// </summary>
    /// <param name="orders">List of orders to filter</param>
    /// <param name="minAmount">Minimum amount filter (inclusive)</param>
    /// <param name="maxAmount">Maximum amount filter (inclusive)</param>
    /// <returns>Filtered list of orders</returns>
    private static List<Order> ApplyAmountRangeFilter(List<Order> orders, decimal? minAmount, decimal? maxAmount)
    {
        if (minAmount.HasValue)
        {
            orders = orders.Where(o => o.TotalAmount >= minAmount.Value).ToList();
        }

        if (maxAmount.HasValue)
        {
            orders = orders.Where(o => o.TotalAmount <= maxAmount.Value).ToList();
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

            "customername" => isDescending
                ? orders.OrderByDescending(o => o.CustomerName).ToList()
                : orders.OrderBy(o => o.CustomerName).ToList(),

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