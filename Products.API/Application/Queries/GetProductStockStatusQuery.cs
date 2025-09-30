using MediatR;

namespace Products.API.Application.Queries;

/// <summary>
/// Query for retrieving product stock status information
/// Useful for inventory dashboard and reporting
/// </summary>
public record GetProductStockStatusQuery : IRequest<ProductStockStatusResult>
{
    /// <summary>
    /// Low stock threshold for categorization
    /// Default is 10
    /// </summary>
    public int LowStockThreshold { get; init; } = 10;

    /// <summary>
    /// Well stocked threshold for categorization
    /// Default is 50
    /// </summary>
    public int WellStockedThreshold { get; init; } = 50;

    /// <summary>
    /// Whether to include soft deleted products in statistics
    /// Default is false
    /// </summary>
    public bool IncludeDeleted { get; init; } = false;

    /// <summary>
    /// User requesting the stock status (for audit/logging purposes)
    /// </summary>
    public string? RequestedBy { get; init; }
}

/// <summary>
/// Result object for product stock status queries
/// Provides aggregate information about inventory levels
/// </summary>
/// <param name="TotalProducts">Total number of products</param>
/// <param name="OutOfStockCount">Number of products with zero stock</param>
/// <param name="LowStockCount">Number of products with low stock</param>
/// <param name="InStockCount">Number of products with adequate stock</param>
/// <param name="WellStockedCount">Number of products with high stock levels</param>
public record ProductStockStatusResult(
    int TotalProducts,
    int OutOfStockCount,
    int LowStockCount,
    int InStockCount,
    int WellStockedCount
)
{
    /// <summary>
    /// Total monetary value of all inventory
    /// </summary>
    public decimal TotalInventoryValue { get; init; }

    /// <summary>
    /// Average stock level across all products
    /// </summary>
    public double AverageStockLevel { get; init; }

    /// <summary>
    /// Percentage of products that are out of stock
    /// </summary>
    public double OutOfStockPercentage => TotalProducts == 0 ? 0 : Math.Round((double)OutOfStockCount / TotalProducts * 100, 2);

    /// <summary>
    /// Percentage of products with low stock
    /// </summary>
    public double LowStockPercentage => TotalProducts == 0 ? 0 : Math.Round((double)LowStockCount / TotalProducts * 100, 2);
}