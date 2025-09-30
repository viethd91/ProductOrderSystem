namespace Products.API.Application.DTOs;

/// <summary>
/// Data Transfer Object for Product entity
/// Used for API responses and CQRS command results
/// </summary>
public record ProductDto
{
    /// <summary>
    /// Product unique identifier
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Product name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Product price
    /// </summary>
    public required decimal Price { get; init; }

    /// <summary>
    /// Available stock quantity
    /// </summary>
    public required int Stock { get; init; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public required DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Whether the product is currently available (stock > 0 and not deleted)
    /// </summary>
    public required bool IsAvailable { get; init; }

    /// <summary>
    /// Whether the product has been soft deleted
    /// </summary>
    public required bool IsDeleted { get; init; }

    /// <summary>
    /// Optional product description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional product category
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Formatted price for display purposes
    /// </summary>
    public string FormattedPrice => Price.ToString("C");

    /// <summary>
    /// Stock status indicator
    /// </summary>
    public string StockStatus => Stock switch
    {
        0 => "Out of Stock",
        <= 10 => "Low Stock",
        <= 50 => "In Stock",
        _ => "Well Stocked"
    };
}