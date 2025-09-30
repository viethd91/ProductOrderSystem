using FluentValidation;
using Orders.API.Application.Commands;

namespace Orders.API.Application.Validators;

/// <summary>
/// FluentValidation validator for CancelOrderCommand
/// Validates order cancellation requests with business rule enforcement
/// </summary>
public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    /// <summary>
    /// Constructor defining validation rules for CancelOrderCommand
    /// </summary>
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required and cannot be empty.")
            .WithErrorCode("ORDER_ID_REQUIRED");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required and cannot be empty.")
            .WithErrorCode("CANCELLATION_REASON_REQUIRED")
            .MaximumLength(500)
            .WithMessage("Cancellation reason cannot exceed 500 characters.")
            .WithErrorCode("CANCELLATION_REASON_TOO_LONG")
            .MinimumLength(10)
            .WithMessage("Cancellation reason must be at least 10 characters to provide meaningful context.")
            .WithErrorCode("CANCELLATION_REASON_TOO_SHORT");

        // Optional fields validation
        RuleFor(x => x.CancelledBy)
            .MaximumLength(200)
            .WithMessage("Cancelled by field cannot exceed 200 characters.")
            .WithErrorCode("CANCELLED_BY_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.CancelledBy));

        RuleFor(x => x.CancellationDetails)
            .MaximumLength(1000)
            .WithMessage("Cancellation details cannot exceed 1000 characters.")
            .WithErrorCode("CANCELLATION_DETAILS_TOO_LONG")
            .When(x => !string.IsNullOrEmpty(x.CancellationDetails));

        // Business rule: Validate cancellation reason content
        RuleFor(x => x.Reason)
            .Must(BeValidCancellationReason)
            .WithMessage("Cancellation reason must provide meaningful context and cannot contain only whitespace or repeated characters.")
            .WithErrorCode("INVALID_CANCELLATION_REASON_CONTENT")
            .When(x => !string.IsNullOrEmpty(x.Reason));

        // Business rule: Automatic cancellations should not process refund immediately without review
        RuleFor(x => x.ProcessRefundImmediately)
            .Must(x => x == false)
            .WithMessage("Automatic cancellations cannot process refunds immediately. Manual review is required.")
            .WithErrorCode("AUTOMATIC_REFUND_NOT_ALLOWED")
            .When(x => x.IsAutomaticCancellation);

        // Conditional validation for manual cancellations
        RuleFor(x => x.CancelledBy)
            .NotEmpty()
            .WithMessage("'Cancelled by' field is required for manual cancellations.")
            .WithErrorCode("CANCELLED_BY_REQUIRED_FOR_MANUAL")
            .When(x => !x.IsAutomaticCancellation);
    }

    /// <summary>
    /// Custom validation to ensure cancellation reason is meaningful
    /// </summary>
    /// <param name="reason">Cancellation reason to validate</param>
    /// <returns>True if reason is valid, false otherwise</returns>
    private static bool BeValidCancellationReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return false;

        // Check for repeated characters (like "aaaaaaa" or "1111111")
        if (reason.Length >= 5 && reason.All(c => c == reason[0]))
            return false;

        // Check for minimum word count (at least 2 words)
        var words = reason.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2)
            return false;

        // Check for common placeholder text
        var invalidReasons = new[]
        {
            "test", "testing", "placeholder", "temp", "temporary",
            "n/a", "na", "none", "no reason", "cancel", "cancelled"
        };

        var lowerReason = reason.ToLowerInvariant();
        return !invalidReasons.Any(invalid => lowerReason.Contains(invalid) && lowerReason.Length <= invalid.Length + 5);
    }
}