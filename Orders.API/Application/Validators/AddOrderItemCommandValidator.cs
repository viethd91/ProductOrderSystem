using FluentValidation;
using Orders.API.Application.Commands;

namespace Orders.API.Application.Validators;

/// <summary>
/// FluentValidation validator for AddOrderItemCommand
/// Validates requests to add items to existing orders
/// </summary>
public class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
{
    /// <summary>
    /// Constructor defining validation rules for AddOrderItemCommand
    /// </summary>
    public AddOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required and cannot be empty.")
            .WithErrorCode("ORDER_ID_REQUIRED");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required and cannot be empty.")
            .WithErrorCode("PRODUCT_ID_REQUIRED");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required and cannot be empty.")
            .WithErrorCode("PRODUCT_NAME_REQUIRED")
            .MaximumLength(200)
            .WithMessage("Product name cannot exceed 200 characters.")
            .WithErrorCode("PRODUCT_NAME_TOO_LONG");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.")
            .WithErrorCode("QUANTITY_INVALID")
            .LessThanOrEqualTo(1000)
            .WithMessage("Quantity cannot exceed 1000 items.")
            .WithErrorCode("QUANTITY_TOO_HIGH");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0)
            .WithMessage("Unit price must be greater than $0.00.")
            .WithErrorCode("UNIT_PRICE_INVALID")
            .LessThanOrEqualTo(1_000_000m)
            .WithMessage("Unit price cannot exceed $1,000,000.00.")
            .WithErrorCode("UNIT_PRICE_TOO_HIGH")
            .ScalePrecision(2, 10)
            .WithMessage("Unit price can have at most 2 decimal places.")
            .WithErrorCode("UNIT_PRICE_PRECISION_INVALID");

        // Optional fields validation
        RuleFor(x => x.AddedBy)
            .MaximumLength(200)
            .WithMessage("Added by field cannot exceed 200 characters.")
            .WithErrorCode("ADDED_BY_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.AddedBy));

        RuleFor(x => x.ItemNotes)
            .MaximumLength(500)
            .WithMessage("Item notes cannot exceed 500 characters.")
            .WithErrorCode("ITEM_NOTES_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.ItemNotes));

        RuleFor(x => x.AdditionReason)
            .MaximumLength(500)
            .WithMessage("Addition reason cannot exceed 500 characters.")
            .WithErrorCode("ADDITION_REASON_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.AdditionReason));

        // Business rule: Total price validation
        RuleFor(x => x)
            .Must(x => x.TotalPrice <= 100_000m)
            .WithMessage("Total price for a single line item cannot exceed $100,000.00.")
            .WithErrorCode("LINE_ITEM_TOTAL_TOO_HIGH");
    }
}