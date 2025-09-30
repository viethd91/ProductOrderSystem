namespace Orders.API.Application.DTOs;

/// <summary>
/// Data Transfer Object for OrderItem entity
/// Used for API responses and cross-boundary communication
/// </summary>
public record OrderItemDto
{
    /// <summary>
    /// Unique identifier for the order item
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Product identifier from the Products catalog
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// Product name at the time of order (for historical purposes)
    /// </summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>
    /// Quantity of the product ordered
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Unit price of the product at the time of order
    /// </summary>
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// Subtotal for this line item (Quantity × UnitPrice)
    /// </summary>
    public decimal Subtotal { get; init; }

    /// <summary>
    /// Total price for this line item (alias for Subtotal for clarity)
    /// </summary>
    public decimal TotalPrice => Subtotal;

    /// <summary>
    /// Unit price formatted as currency
    /// </summary>
    public string UnitPriceFormatted => UnitPrice.ToString("C");

    /// <summary>
    /// Subtotal formatted as currency
    /// </summary>
    public string SubtotalFormatted => Subtotal.ToString("C");

    /// <summary>
    /// Item description for display purposes
    /// </summary>
    public string ItemDescription => $"{ProductName} (Qty: {Quantity} × {UnitPriceFormatted} = {SubtotalFormatted})";

    /// <summary>
    /// Indicates if this is a high-value item (subtotal > $1000)
    /// </summary>
    public bool IsHighValueItem => Subtotal > 1000m;

    /// <summary>
    /// Indicates if this is a bulk order item (quantity > 10)
    /// </summary>
    public bool IsBulkOrder => Quantity > 10;
}