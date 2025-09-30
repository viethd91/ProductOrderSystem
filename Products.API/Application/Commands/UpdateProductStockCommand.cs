using MediatR;
using Products.API.Application.DTOs;

namespace Products.API.Application.Commands;

/// <summary>
/// Specialized command for updating product stock levels
/// Supports both absolute stock updates and relative adjustments
/// </summary>
/// <param name="Id">Product unique identifier</param>
/// <param name="NewStock">New stock quantity (must be non-negative)</param>
public record UpdateProductStockCommand(
    Guid Id,
    int NewStock
) : IRequest<ProductDto>
{
    /// <summary>
    /// Whether this is a relative adjustment (+/-) or absolute value
    /// Default is false (absolute value)
    /// </summary>
    public bool IsRelativeAdjustment { get; init; } = false;

    /// <summary>
    /// Reason for stock change (restocking, sales, damage, etc.)
    /// </summary>
    public string? StockChangeReason { get; init; }

    /// <summary>
    /// User or system performing the stock update
    /// </summary>
    public string? UpdatedBy { get; init; }

    /// <summary>
    /// Reference number for the stock transaction (PO, adjustment ID, etc.)
    /// </summary>
    public string? ReferenceNumber { get; init; }
}