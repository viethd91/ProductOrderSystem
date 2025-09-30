using MediatR;
using Products.API.Application.DTOs;

namespace Products.API.Application.Commands;

/// <summary>
/// Specialized command for updating only the product price
/// Useful for price management scenarios and raises specific domain events
/// </summary>
/// <param name="Id">Product unique identifier</param>
/// <param name="NewPrice">New price for the product (must be positive)</param>
public record UpdateProductPriceCommand(
    Guid Id,
    decimal NewPrice
) : IRequest<ProductDto>
{
    /// <summary>
    /// Reason for price change (for audit and business analysis)
    /// </summary>
    public string? PriceChangeReason { get; init; }

    /// <summary>
    /// User or system performing the price update
    /// </summary>
    public string? UpdatedBy { get; init; }

    /// <summary>
    /// Effective date for the price change (default is now)
    /// </summary>
    public DateTime? EffectiveDate { get; init; }
}