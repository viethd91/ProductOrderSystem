using MediatR;
using Products.API.Application.DTOs;

namespace Products.API.Application.Queries;

/// <summary>
/// Query for retrieving a single product by its unique identifier
/// Follows CQRS pattern with MediatR for read operations
/// </summary>
/// <param name="Id">Product unique identifier</param>
public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>
{
    /// <summary>
    /// Whether to include soft deleted products in the search
    /// Default is false (only active products)
    /// </summary>
    public bool IncludeDeleted { get; init; } = false;

    /// <summary>
    /// User requesting the product (for audit/logging purposes)
    /// </summary>
    public string? RequestedBy { get; init; }
}