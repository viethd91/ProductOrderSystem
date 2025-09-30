namespace Orders.API.Application.DTOs;

/// <summary>
/// Data Transfer Object for OrderItem entity
/// Used for API responses and nested in OrderDto
/// </summary>
public record OrderItemDto
{
    /// <summary>
    /// Order item unique identifier
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Product identifier
    /// </summary>
    public required Guid ProductId { get; init; }

    /// <summary>
    /// Product name at time of order
    /// </summary>
    public required string ProductName { get; init; }

    /// <summary>
    /// Unit price at time of order
    /// </summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>
    /// Quantity ordered
    /// </summary>
    public required int Quantity { get; init; }

    /// <summary>
    /// Total price for this line item
    /// </summary>
    public decimal TotalPrice { get; init; }

    /// <summary>
    /// Unit price formatted as currency
    /// </summary>
    public string FormattedUnitPrice { get; init; } = string.Empty;

    /// <summary>
    /// Total price formatted as currency
    /// </summary>
    public string FormattedTotalPrice { get; init; } = string.Empty;
}