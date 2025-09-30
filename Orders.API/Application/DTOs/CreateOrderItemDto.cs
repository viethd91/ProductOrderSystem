namespace Orders.API.Application.DTOs;

/// <summary>
/// Data Transfer Object for creating order items within an order
/// Used in CreateOrderCommand and AddOrderItemCommand
/// </summary>
public record CreateOrderItemDto
{
    /// <summary>
    /// Product identifier from the Products catalog
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// Product name for display and historical purposes
    /// </summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>
    /// Quantity to order (must be positive)
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Unit price at time of order (must be positive)
    /// </summary>
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// Calculated total price for this line item
    /// </summary>
    public decimal TotalPrice => Quantity * UnitPrice;

    /// <summary>
    /// Optional notes for this specific item
    /// </summary>
    public string? ItemNotes { get; init; }

    /// <summary>
    /// Unit price formatted as currency
    /// </summary>
    public string UnitPriceFormatted => UnitPrice.ToString("C");

    /// <summary>
    /// Total price formatted as currency
    /// </summary>
    public string TotalPriceFormatted => TotalPrice.ToString("C");

    /// <summary>
    /// Item summary for display purposes
    /// </summary>
    public string ItemSummary => $"{ProductName} - Qty: {Quantity} × {UnitPriceFormatted} = {TotalPriceFormatted}";

    /// <summary>
    /// Validates that the CreateOrderItemDto has valid values
    /// </summary>
    public bool IsValid => ProductId != Guid.Empty &&
                          !string.IsNullOrWhiteSpace(ProductName) &&
                          Quantity > 0 &&
                          UnitPrice > 0;

    /// <summary>
    /// Gets validation errors if the DTO is invalid
    /// </summary>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (ProductId == Guid.Empty)
            errors.Add("Product ID is required");

        if (string.IsNullOrWhiteSpace(ProductName))
            errors.Add("Product name is required");
        else if (ProductName.Length > 200)
            errors.Add("Product name cannot exceed 200 characters");

        if (Quantity <= 0)
            errors.Add("Quantity must be greater than zero");
        else if (Quantity > 10000)
            errors.Add("Quantity cannot exceed 10,000 items");

        if (UnitPrice <= 0)
            errors.Add("Unit price must be greater than zero");
        else if (UnitPrice > 1_000_000m)
            errors.Add("Unit price cannot exceed 1,000,000");

        return errors;
    }
}