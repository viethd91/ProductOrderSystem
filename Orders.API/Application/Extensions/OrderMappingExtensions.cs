using Orders.API.Application.DTOs;
using Orders.API.Domain.Entities;

namespace Orders.API.Application.Extensions;

/// <summary>
/// Extension methods for mapping between Order domain entities and DTOs
/// Provides manual mapping to avoid AutoMapper dependency in simple scenarios
/// </summary>
public static class OrderMappingExtensions
{
    /// <summary>
    /// Converts Order entity to OrderDto
    /// </summary>
    /// <param name="order">Order entity</param>
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
            OrderItems = order.OrderItems.Select(oi => oi.ToDto()).ToList(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }

    /// <summary>
    /// Converts OrderItem entity to OrderItemDto
    /// </summary>
    /// <param name="orderItem">OrderItem entity</param>
    /// <returns>OrderItemDto representation</returns>
    public static OrderItemDto ToDto(this OrderItem orderItem)
    {
        ArgumentNullException.ThrowIfNull(orderItem);

        return new OrderItemDto
        {
            Id = orderItem.Id,
            ProductId = orderItem.ProductId,
            ProductName = orderItem.ProductName,
            Quantity = orderItem.Quantity,
            UnitPrice = orderItem.UnitPrice,
            Subtotal = orderItem.TotalPrice
        };
    }

    /// <summary>
    /// Converts CreateOrderItemDto to OrderItem entity
    /// </summary>
    /// <param name="dto">CreateOrderItemDto</param>
    /// <returns>OrderItem entity</returns>
    public static OrderItem ToEntity(this CreateOrderItemDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new OrderItem(
            dto.ProductId,
            dto.ProductName,
            dto.Quantity,
            dto.UnitPrice
        );
    }

    /// <summary>
    /// Converts collection of CreateOrderItemDto to OrderItem entities
    /// </summary>
    /// <param name="dtos">Collection of CreateOrderItemDto</param>
    /// <returns>Collection of OrderItem entities</returns>
    public static List<OrderItem> ToEntities(this IEnumerable<CreateOrderItemDto> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        return dtos.Select(dto => dto.ToEntity()).ToList();
    }
}