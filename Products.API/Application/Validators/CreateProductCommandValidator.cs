using FluentValidation;
using Products.API.Application.Commands;
using Products.API.Domain.Interfaces;

namespace Products.API.Application.Validators;

/// <summary>
/// FluentValidation validator for CreateProductCommand
/// Ensures business rules are enforced at the application boundary
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private readonly IProductRepository _repository;

    /// <summary>
    /// Constructor with repository dependency for async validation
    /// </summary>
    /// <param name="repository">Product repository for business rule validation</param>
    public CreateProductCommandValidator(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        ConfigureValidationRules();
    }

    /// <summary>
    /// Configures all validation rules for CreateProductCommand
    /// </summary>
    private void ConfigureValidationRules()
    {
        // Name validation rules
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(200)
            .WithMessage("Product name cannot exceed 200 characters.")
            .Matches("^[a-zA-Z0-9\\s\\-_.,()&]+$")
            .WithMessage("Product name contains invalid characters.")
            .MustAsync(BeUniqueProductName)
            .WithMessage("A product with this name already exists.");

        // Price validation rules
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Product price must be greater than zero.")
            .LessThan(999999.99m)
            .WithMessage("Product price cannot exceed $999,999.99.")
            .Must(HaveValidDecimalPlaces)
            .WithMessage("Product price can have at most 2 decimal places.");

        // Stock validation rules
        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Product stock cannot be negative.")
            .LessThan(1000000)
            .WithMessage("Product stock cannot exceed 999,999 units.");

        // Optional field validations
        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Product description cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Category)
            .MaximumLength(100)
            .WithMessage("Product category cannot exceed 100 characters.")
            .Matches("^[a-zA-Z\\s\\-_]+$")
            .WithMessage("Product category contains invalid characters.")
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.CreatedBy)
            .MaximumLength(200)
            .WithMessage("Created by field cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.CreatedBy));
    }

    /// <summary>
    /// Validates that the product name is unique
    /// </summary>
    /// <param name="name">Product name to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if name is unique, false otherwise</returns>
    private async Task<bool> BeUniqueProductName(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true; // Let the NotEmpty rule handle this

        return !await _repository.IsNameTakenAsync(name, cancellationToken: cancellationToken);
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