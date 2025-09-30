using MediatR;
using Products.API.Application.DTOs;

namespace Products.API.Application.Queries;

/// <summary>
/// Query for retrieving all products
/// Follows CQRS pattern with MediatR for read operations
/// </summary>
public record GetAllProductsQuery : IRequest<List<ProductDto>>
{
    /// <summary>
    /// Whether to include soft deleted products
    /// Default is false (only active products)
    /// </summary>
    public bool IncludeDeleted { get; init; } = false;

    /// <summary>
    /// Optional sorting criteria (Name, Price, Stock, CreatedAt)
    /// Default is "Name" ascending
    /// </summary>
    public string SortBy { get; init; } = "Name";

    /// <summary>
    /// Sort direction (asc, desc)
    /// Default is ascending
    /// </summary>
    public string SortDirection { get; init; } = "asc";

    /// <summary>
    /// User requesting the products (for audit/logging purposes)
    /// </summary>
    public string? RequestedBy { get; init; }
}