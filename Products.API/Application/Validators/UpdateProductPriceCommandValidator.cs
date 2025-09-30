using FluentValidation;
using Products.API.Application.Commands;
using Products.API.Domain.Interfaces;

namespace Products.API.Application.Validators;

/// <summary>
/// FluentValidation validator for UpdateProductPriceCommand
/// Specialized validation for price-only updates
/// </summary>
public class UpdateProductPriceCommandValidator : AbstractValidator<UpdateProductPriceCommand>
{
    private readonly IProductRepository _repository;

    /// <summary>
    /// Constructor with repository dependency for async validation
    /// </summary>
    /// <param name="repository">Product repository for business rule validation</param>
    public UpdateProductPriceCommandValidator(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        ConfigureValidationRules();
    }

    /// <summary>
    /// Configures all validation rules for UpdateProductPriceCommand
    /// </summary>
    private void ConfigureValidationRules()
    {
        // ID validation
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Product ID is required.")
            .MustAsync(ProductMustExist)
            .WithMessage("Product with the specified ID does not exist.")
            .MustAsync(ProductMustNotBeDeleted)
            .WithMessage("Cannot update price for a deleted product.");

        // Price validation rules
        RuleFor(x => x.NewPrice)
            .GreaterThan(0)
            .WithMessage("Product price must be greater than zero.")
            .LessThan(999999.99m)
            .WithMessage("Product price cannot exceed $999,999.99.")
            .Must(HaveValidDecimalPlaces)
            .WithMessage("Product price can have at most 2 decimal places.")
            .MustAsync(BeDifferentFromCurrentPrice)
            .WithMessage("New price must be different from the current price.");

        // Optional field validations
        RuleFor(x => x.PriceChangeReason)
            .MaximumLength(500)
            .WithMessage("Price change reason cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.PriceChangeReason));

        RuleFor(x => x.UpdatedBy)
            .MaximumLength(200)
            .WithMessage("Updated by field cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.UpdatedBy));

        // Effective date validation
        RuleFor(x => x.EffectiveDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Effective date cannot be in the past.")
            .When(x => x.EffectiveDate.HasValue);
    }

    /// <summary>
    /// Validates that the product exists
    /// </summary>
    /// <param name="id">Product ID to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if product exists, false otherwise</returns>
    private async Task<bool> ProductMustExist(Guid id, CancellationToken cancellationToken)
    {
        return await _repository.ExistsAsync(id, cancellationToken);
    }

    /// <summary>
    /// Validates that the product is not deleted
    /// </summary>
    /// <param name="id">Product ID to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if product is not deleted, false otherwise</returns>
    private async Task<bool> ProductMustNotBeDeleted(Guid id, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        return product != null && !product.IsDeleted;
    }

    /// <summary>
    /// Validates that the new price is different from the current price
    /// </summary>
    /// <param name="command">The price update command</param>
    /// <param name="newPrice">New price to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if price is different, false otherwise</returns>
    private async Task<bool> BeDifferentFromCurrentPrice(
        UpdateProductPriceCommand command,
        decimal newPrice,
        CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(command.Id, cancellationToken);
        return product == null || product.Price != newPrice;
    }

    /// <summary>
    /// Validates that the price has at most 2 decimal places
    /// </summary>
    /// <param name="price">Price to validate</param>
    /// <returns>True if price has valid decimal places</returns>
    private static bool HaveValidDecimalPlaces(decimal price)
    {
        return decimal.Round(price, 2) == price;
    }
}