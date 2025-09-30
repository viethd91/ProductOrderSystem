using MediatR;

namespace Products.API.Application.Commands;

/// <summary>
/// Command for bulk operations on multiple products
/// Useful for batch processing and administrative operations
/// </summary>
/// <param name="ProductIds">List of product IDs to update</param>
/// <param name="Operation">Type of bulk operation to perform</param>
public record BulkUpdateProductsCommand(
    IEnumerable<Guid> ProductIds,
    BulkOperationType Operation
) : IRequest<BulkUpdateResult>
{
    /// <summary>
    /// New price for bulk price updates
    /// </summary>
    public decimal? NewPrice { get; init; }

    /// <summary>
    /// Stock adjustment amount for bulk stock operations
    /// </summary>
    public int? StockAdjustment { get; init; }

    /// <summary>
    /// Percentage for price adjustments (e.g., 10% increase)
    /// </summary>
    public decimal? PriceAdjustmentPercentage { get; init; }

    /// <summary>
    /// Reason for bulk operation
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// User performing the bulk operation
    /// </summary>
    public string? UpdatedBy { get; init; }
}

/// <summary>
/// Types of bulk operations supported
/// </summary>
public enum BulkOperationType
{
    UpdatePrice,
    AdjustStock,
    ApplyPricePercentage,
    SoftDelete,
    Activate,
    UpdateCategory
}

/// <summary>
/// Result of bulk update operation
/// </summary>
/// <param name="SuccessCount">Number of products successfully updated</param>
/// <param name="FailureCount">Number of products that failed to update</param>
/// <param name="Errors">List of errors encountered during bulk operation</param>
public record BulkUpdateResult(
    int SuccessCount,
    int FailureCount,
    IEnumerable<string> Errors
);