using MediatR;
using Products.API.Application.DTOs;

namespace Products.API.Application.Queries;

/// <summary>
/// Query for retrieving products with low stock levels
/// Useful for inventory management and restocking alerts
/// </summary>
public record GetLowStockProductsQuery : IRequest<List<ProductDto>>
{
    /// <summary>
    /// Stock threshold below which products are considered low stock
    /// Default is 10
    /// </summary>
    public int Threshold { get; init; } = 10;

    /// <summary>
    /// Whether to include out-of-stock products (stock = 0)
    /// Default is true
    /// </summary>
    public bool IncludeOutOfStock { get; init; } = true;

    /// <summary>
    /// Whether to include soft deleted products
    /// Default is false (only active products)
    /// </summary>
    public bool IncludeDeleted { get; init; } = false;

    /// <summary>
    /// User requesting the low stock report (for audit/logging purposes)
    /// </summary>
    public string? RequestedBy { get; init; }
}