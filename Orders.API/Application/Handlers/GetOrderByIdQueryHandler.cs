using MediatR;
using Orders.API.Application.DTOs;
using Orders.API.Application.Extensions;
using Orders.API.Application.Queries;
using Orders.API.Domain.Enums;
using Orders.API.Domain.Interfaces;

namespace Orders.API.Application.Handlers;

/// <summary>
/// Query handler for retrieving a single order by ID
/// Implements CQRS pattern with MediatR for read-only operations
/// </summary>
/// <param name="repository">Order repository for data access</param>
/// <param name="logger">Logger for diagnostics and monitoring</param>
public class GetOrderByIdQueryHandler(
    IOrderRepository repository,
    ILogger<GetOrderByIdQueryHandler> logger) : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    /// <summary>
    /// Handles the get order by ID query
    /// </summary>
    /// <param name="request">Query request with order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OrderDto if found, otherwise null</returns>
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogDebug("Retrieving order by ID: {OrderId}. Requested by: {RequestedBy}, Include cancelled: {IncludeCancelled}",
            request.Id, request.RequestedBy ?? "Unknown", request.IncludeCancelled);

        try
        {
            // Get order from repository (includes OrderItems and applies global query filter for cancelled orders)
            var order = await repository.GetByIdAsync(request.Id, cancellationToken);

            if (order == null)
            {
                logger.LogDebug("Order {OrderId} not found", request.Id);
                return null;
            }

            // Check if order is cancelled and if we should include it
            if (order.Status == OrderStatus.Cancelled && !request.IncludeCancelled)
            {
                logger.LogDebug("Order {OrderId} is cancelled and IncludeCancelled is false, returning null", request.Id);
                return null;
            }

            logger.LogDebug("Found order {OrderId} ({OrderNumber}) for customer {CustomerName} with {ItemCount} items",
                order.Id, order.OrderNumber, order.CustomerName, order.OrderItems.Count);

            // Map domain entity to DTO using extension method
            var orderDto = order.ToDto();

            logger.LogDebug("Successfully retrieved order {OrderId}", request.Id);

            return orderDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order {OrderId}: {ErrorMessage}",
                request.Id, ex.Message);
            throw;
        }
    }
}