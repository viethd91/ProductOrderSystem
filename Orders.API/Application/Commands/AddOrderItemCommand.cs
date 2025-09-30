using MediatR;
using Orders.API.Application.DTOs;

namespace Orders.API.Application.Commands;

/// <summary>
/// Command for adding an item to an existing order
/// Can only be performed on orders in Pending status
/// </summary>
/// <param name="OrderId">Order unique identifier to add item to</param>
/// <param name="ProductId">Product identifier from catalog</param>
/// <param name="ProductName">Product name for display and historical purposes</param>
/// <param name="Quantity">Quantity to add (must be positive)</param>
/// <param name="UnitPrice">Unit price at time of addition (must be positive)</param>
public record AddOrderItemCommand(
    Guid OrderId,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
) : IRequest<OrderDto>
{
    /// <summary>
    /// User or system adding the item
    /// </summary>
    public string? AddedBy { get; init; }

    /// <summary>
    /// Optional notes for this specific item
    /// </summary>
    public string? ItemNotes { get; init; }

    /// <summary>
    /// Whether to merge with existing item if product already exists in order
    /// Default is true - will increase quantity of existing item
    /// </summary>
    public bool MergeIfExists { get; init; } = true;

    /// <summary>
    /// Reason for adding the item (for audit purposes)
    /// </summary>
    public string? AdditionReason { get; init; }

    /// <summary>
    /// Calculated total price for this line item
    /// </summary>
    public decimal TotalPrice => Quantity * UnitPrice;
}