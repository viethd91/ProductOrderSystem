using MediatR;
using Products.API.Application.DTOs;

namespace Products.API.Application.Commands;

/// <summary>
/// Command for creating a new product
/// Follows CQRS pattern with MediatR
/// </summary>
/// <param name="Name">Product name (required, max 200 chars)</param>
/// <param name="Price">Product price (must be positive)</param>
/// <param name="Stock">Product stock quantity (must be non-negative)</param>
public record CreateProductCommand(
    string Name,
    decimal Price,
    int Stock
) : IRequest<ProductDto>
{
    /// <summary>
    /// Optional product description for future extensibility
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional category for the product
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Metadata for audit purposes
    /// </summary>
    public string? CreatedBy { get; init; }
}