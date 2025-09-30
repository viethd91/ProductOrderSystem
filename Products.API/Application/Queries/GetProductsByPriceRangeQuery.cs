using MediatR;
using Products.API.Application.DTOs;

namespace Products.API.Application.Queries;

/// <summary>
/// Query for retrieving products within a specific price range
/// Useful for filtering products by price
/// </summary>
/// <param name="MinPrice">Minimum price (inclusive)</param>
/// <param name="MaxPrice">Maximum price (inclusive)</param>
public record GetProductsByPriceRangeQuery(
    decimal MinPrice,
    decimal MaxPrice
) : IRequest<List<ProductDto>>
{
    /// <summary>
    /// Whether to include soft deleted products
    /// Default is false (only active products)
    /// </summary>
    public bool IncludeDeleted { get; init; } = false;

    /// <summary>
    /// Sort by price (asc/desc) or name
    /// Default is "Price"
    /// </summary>
    public string SortBy { get; init; } = "Price";

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