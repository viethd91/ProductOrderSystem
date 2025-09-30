using Orders.API.Application.DTOs;
using Orders.API.Domain.Entities;

namespace Orders.API.Application.Extensions;

/// <summary>
/// Extension methods for mapping between Order entities and DTOs
/// Provides clean mapping without external dependencies like AutoMapper
/// </summary>
public static class OrderMappingExtensions
{
    /// <summary>
    /// Maps an Order entity to OrderDto
    /// </summary>
    /// <param name="order">Order entity to map</param>
    /// <returns>OrderDto representation</returns>
    public static OrderDto ToDto(this Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            OrderDate = order.OrderDate,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            OrderItems = order.OrderItems.Select(item => item.ToDto()).ToList(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            ItemCount = order.OrderItems.Count,
            IsModifiable = order.CanBeModified,
            IsFinalState = order.IsFinalState,
            CanBeCancelled = order.CanBeCancelled
        };
    }

    /// <summary>
    /// Maps an OrderItem entity to OrderItemDto
    /// </summary>
    /// <param name="orderItem">OrderItem entity to map</param>
    /// <returns>OrderItemDto representation</returns>
    public static OrderItemDto ToDto(this OrderItem orderItem)
    {
        ArgumentNullException.ThrowIfNull(orderItem);

        return new OrderItemDto
        {
            Id = orderItem.Id,
            ProductId = orderItem.ProductId,
            ProductName = orderItem.ProductName,
            UnitPrice = orderItem.UnitPrice,
            Quantity = orderItem.Quantity,
            TotalPrice = orderItem.TotalPrice,
            FormattedUnitPrice = orderItem.UnitPrice.ToString("C"),
            FormattedTotalPrice = orderItem.TotalPrice.ToString("C")
        };
    }

    /// <summary>
    /// Maps a collection of Order entities to OrderDto collection
    /// </summary>
    /// <param name="orders">Collection of Order entities</param>
    /// <returns>Collection of OrderDto</returns>
    public static IEnumerable<OrderDto> ToDto(this IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);
        return orders.Select(order => order.ToDto());
    }

    /// <summary>
    /// Maps an Order entity to OrderSummaryDto (lightweight version)
    /// </summary>
    /// <param name="order">Order entity to map</param>
    /// <returns>OrderSummaryDto representation</returns>
    public static OrderSummaryDto ToSummaryDto(this Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return new OrderSummaryDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.CustomerName,
            OrderDate = order.OrderDate,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            ItemCount = order.OrderItems.Count,
            FormattedTotalAmount = order.TotalAmount.ToString("C"),
            OrderAge = DateTime.UtcNow - order.OrderDate
        };
    }

    /// <summary>
    /// Maps a collection of Order entities to OrderSummaryDto collection
    /// </summary>
    /// <param name="orders">Collection of Order entities</param>
    /// <returns>Collection of OrderSummaryDto</returns>
    public static IEnumerable<OrderSummaryDto> ToSummaryDto(this IEnumerable<Order> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);
        return orders.Select(order => order.ToSummaryDto());
    }
}