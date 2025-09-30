using MediatR;
using Products.API.Application.DTOs;

namespace Products.API.Application.Commands;

/// <summary>
/// Command for updating an existing product
/// Follows CQRS pattern with MediatR
/// </summary>
/// <param name="Id">Product unique identifier</param>
/// <param name="Name">Updated product name (required, max 200 chars)</param>
/// <param name="Price">Updated product price (must be positive)</param>
/// <param name="Stock">Updated product stock quantity (must be non-negative)</param>
public record UpdateProductCommand(
    Guid Id,
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
    public string? UpdatedBy { get; init; }

    /// <summary>
    /// Reason for the update (for audit trail)
    /// </summary>
    public string? UpdateReason { get; init; }
}