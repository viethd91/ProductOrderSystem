using FluentValidation;
using Orders.API.Application.Commands;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Enums;

namespace Orders.API.Application.Validators;

/// <summary>
/// FluentValidation validator for UpdateOrderStatusCommand
/// Validates order status updates with business rule enforcement
/// </summary>
public class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    /// <summary>
    /// Constructor defining validation rules for UpdateOrderStatusCommand
    /// </summary>
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required and cannot be empty.")
            .WithErrorCode("ORDER_ID_REQUIRED");

        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .WithMessage("Order status must be a valid status value (Pending, Confirmed, Shipped, Delivered, or Cancelled).")
            .WithErrorCode("INVALID_ORDER_STATUS");

        // Optional fields validation
        RuleFor(x => x.StatusChangeReason)
            .MaximumLength(500)
            .WithMessage("Status change reason cannot exceed 500 characters.")
            .WithErrorCode("STATUS_REASON_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.StatusChangeReason));

        RuleFor(x => x.UpdatedBy)
            .MaximumLength(200)
            .WithMessage("Updated by field cannot exceed 200 characters.")
            .WithErrorCode("UPDATED_BY_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.UpdatedBy));

        RuleFor(x => x.TrackingNumber)
            .MaximumLength(100)
            .WithMessage("Tracking number cannot exceed 100 characters.")
            .WithErrorCode("TRACKING_NUMBER_TOO_LONG")
            .Matches(@"^[A-Za-z0-9\-]+$")
            .WithMessage("Tracking number can only contain letters, numbers, and hyphens.")
            .WithErrorCode("TRACKING_NUMBER_INVALID_FORMAT")
            .When(x => !string.IsNullOrEmpty(x.TrackingNumber));

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters.")
            .WithErrorCode("NOTES_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.EstimatedDeliveryDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Estimated delivery date must be in the future.")
            .WithErrorCode("INVALID_DELIVERY_DATE")
            .When(x => x.EstimatedDeliveryDate.HasValue);

        // Business rule: Require tracking number for shipped status
        RuleFor(x => x.TrackingNumber)
            .NotEmpty()
            .WithMessage("Tracking number is required when updating order status to 'Shipped'.")
            .WithErrorCode("TRACKING_NUMBER_REQUIRED_FOR_SHIPPED")
            .When(x => x.NewStatus == OrderStatus.Shipped);

        // Business rule: Require status change reason for cancellation
        RuleFor(x => x.StatusChangeReason)
            .NotEmpty()
            .WithMessage("Status change reason is required when updating order status to 'Cancelled'.")
            .WithErrorCode("REASON_REQUIRED_FOR_CANCELLATION")
            .When(x => x.NewStatus == OrderStatus.Cancelled);

        // Business rule: Validate status progression
        RuleFor(x => x)
            .Must(HaveValidStatusProgression)
            .WithMessage("Invalid status transition. Please ensure the status change follows the correct order lifecycle.")
            .WithErrorCode("INVALID_STATUS_TRANSITION")
            .When(x => x.NewStatus != OrderStatus.Cancelled); // Cancellation can happen from most states
    }

    /// <summary>
    /// Custom validation rule for status progression logic
    /// Note: This is basic validation - detailed transition rules are enforced in the domain
    /// </summary>
    /// <param name="command">The update command to validate</param>
    /// <returns>True if status transition appears valid, false otherwise</returns>
    private static bool HaveValidStatusProgression(UpdateOrderStatusCommand command)
    {
        // Basic progression validation - detailed rules in domain layer
        return command.NewStatus switch
        {
            OrderStatus.Pending => true, // Can always go back to pending (rare but possible)
            OrderStatus.Confirmed => true, // Can be confirmed from pending
            OrderStatus.Shipped => command.TrackingNumber != null, // Must have tracking
            OrderStatus.Delivered => true, // Can be delivered from shipped
            OrderStatus.Cancelled => !string.IsNullOrEmpty(command.StatusChangeReason), // Must have reason
            _ => false
        };
    }
}