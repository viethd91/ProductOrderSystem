using MediatR;
using Products.API.Application.DTOs;

namespace Products.API.Application.Queries;

/// <summary>
/// Query for searching products by name (partial match)
/// Useful for product search functionality
/// </summary>
/// <param name="SearchTerm">Product name search term (partial match, case-insensitive)</param>
public record GetProductsByNameQuery(string SearchTerm) : IRequest<List<ProductDto>>
{
    /// <summary>
    /// Whether to include soft deleted products in search results
    /// Default is false (only active products)
    /// </summary>
    public bool IncludeDeleted { get; init; } = false;

    /// <summary>
    /// Maximum number of results to return (for performance)
    /// Default is 100
    /// </summary>
    public int MaxResults { get; init; } = 100;

    /// <summary>
    /// User performing the search (for audit/logging purposes)
    /// </summary>
    public string? RequestedBy { get; init; }
}