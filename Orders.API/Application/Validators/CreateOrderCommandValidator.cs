using FluentValidation;
using Orders.API.Application.Commands;
using Orders.API.Application.DTOs;
using Orders.API.Domain.Entities;

namespace Orders.API.Application.Validators;

/// <summary>
/// FluentValidation validator for CreateOrderCommand
/// Provides comprehensive validation rules with custom error messages
/// </summary>
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    /// <summary>
    /// Constructor defining validation rules for CreateOrderCommand
    /// </summary>
    public CreateOrderCommandValidator()
    {
        // Customer validation rules
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required and cannot be empty.")
            .WithErrorCode("CUSTOMER_ID_REQUIRED");

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage("Customer name is required and cannot be empty.")
            .WithErrorCode("CUSTOMER_NAME_REQUIRED")
            .MaximumLength(200)
            .WithMessage("Customer name cannot exceed 200 characters.")
            .WithErrorCode("CUSTOMER_NAME_TOO_LONG")
            .Matches(@"^[a-zA-Z\s\-\.\']+$")
            .WithMessage("Customer name can only contain letters, spaces, hyphens, dots, and apostrophes.")
            .WithErrorCode("CUSTOMER_NAME_INVALID_CHARACTERS");

        // Order items validation rules
        RuleFor(x => x.OrderItems)
            .NotNull()
            .WithMessage("Order items are required.")
            .WithErrorCode("ORDER_ITEMS_NULL")
            .NotEmpty()
            .WithMessage("Order must contain at least one item.")
            .WithErrorCode("ORDER_ITEMS_EMPTY")
            .Must(items => items.Count <= 100)
            .WithMessage("Order cannot contain more than 100 items.")
            .WithErrorCode("ORDER_ITEMS_TOO_MANY");

        // Individual order item validation
        RuleForEach(x => x.OrderItems)
            .SetValidator(new CreateOrderItemDtoValidator())
            .When(x => x.OrderItems != null);

        // Business rule: Check for duplicate products
        RuleFor(x => x.OrderItems)
            .Must(NotHaveDuplicateProducts)
            .WithMessage("Order cannot contain duplicate products. Please consolidate quantities for the same product.")
            .WithErrorCode("DUPLICATE_PRODUCTS")
            .When(x => x.OrderItems != null && x.OrderItems.Any());

        // Business rule: Validate total order value
        RuleFor(x => x.OrderItems)
            .Must(HaveValidTotalAmount)
            .WithMessage("Order total must be greater than $0.01 and less than $1,000,000.")
            .WithErrorCode("INVALID_ORDER_TOTAL")
            .When(x => x.OrderItems != null && x.OrderItems.Any());

        // Optional fields validation
        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(1000)
            .WithMessage("Special instructions cannot exceed 1000 characters.")
            .WithErrorCode("SPECIAL_INSTRUCTIONS_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.SpecialInstructions));

        RuleFor(x => x.CreatedBy)
            .MaximumLength(200)
            .WithMessage("Created by field cannot exceed 200 characters.")
            .WithErrorCode("CREATED_BY_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.CreatedBy));

        RuleFor(x => x.ExpectedDeliveryDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Expected delivery date must be in the future.")
            .WithErrorCode("INVALID_DELIVERY_DATE")
            .When(x => x.ExpectedDeliveryDate.HasValue);
    }

    /// <summary>
    /// Custom validation rule to check for duplicate products
    /// </summary>
    /// <param name="orderItems">List of order items to validate</param>
    /// <returns>True if no duplicates, false otherwise</returns>
    private static bool NotHaveDuplicateProducts(List<CreateOrderItemDto> orderItems)
    {
        if (orderItems == null || !orderItems.Any())
            return true;

        var productIds = orderItems.Select(item => item.ProductId).ToList();
        return productIds.Count == productIds.Distinct().Count();
    }

    /// <summary>
    /// Custom validation rule to check total order amount
    /// </summary>
    /// <param name="orderItems">List of order items to validate</param>
    /// <returns>True if total amount is within valid range, false otherwise</returns>
    private static bool HaveValidTotalAmount(List<CreateOrderItemDto> orderItems)
    {
        if (orderItems == null || !orderItems.Any())
            return false;

        try
        {
            var totalAmount = orderItems.Sum(item => item.TotalPrice);
            return totalAmount > 0.01m && totalAmount <= 1_000_000m;
        }
        catch
        {
            return false; // Handle potential overflow or calculation errors
        }
    }
}

/// <summary>
/// FluentValidation validator for CreateOrderItemDto
/// Validates individual order items within an order
/// </summary>
public class CreateOrderItemDtoValidator : AbstractValidator<CreateOrderItemDto>
{
    /// <summary>
    /// Constructor defining validation rules for CreateOrderItemDto
    /// </summary>
    public CreateOrderItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required for each order item.")
            .WithErrorCode("PRODUCT_ID_REQUIRED");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required for each order item.")
            .WithErrorCode("PRODUCT_NAME_REQUIRED")
            .MaximumLength(200)
            .WithMessage("Product name cannot exceed 200 characters.")
            .WithErrorCode("PRODUCT_NAME_TOO_LONG");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.")
            .WithErrorCode("QUANTITY_INVALID")
            .LessThanOrEqualTo(1000)
            .WithMessage("Quantity cannot exceed 1000 items per product.")
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

        RuleFor(x => x.ItemNotes)
            .MaximumLength(500)
            .WithMessage("Item notes cannot exceed 500 characters.")
            .WithErrorCode("ITEM_NOTES_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.ItemNotes));
    }
}