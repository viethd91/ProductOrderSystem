using FluentValidation;
using Products.API.Application.Commands;
using Products.API.Domain.Interfaces;

namespace Products.API.Application.Validators;

/// <summary>
/// FluentValidation validator for UpdateProductCommand
/// Ensures business rules are enforced for product updates
/// </summary>
public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private readonly IProductRepository _repository;

    /// <summary>
    /// Constructor with repository dependency for async validation
    /// </summary>
    /// <param name="repository">Product repository for business rule validation</param>
    public UpdateProductCommandValidator(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        ConfigureValidationRules();
    }

    /// <summary>
    /// Configures all validation rules for UpdateProductCommand
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
            .WithMessage("Cannot update a deleted product.");

        // Name validation rules
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(200)
            .WithMessage("Product name cannot exceed 200 characters.")
            .Matches("^[a-zA-Z0-9\\s\\-_.,()&]+$")
            .WithMessage("Product name contains invalid characters.")
            .MustAsync(BeUniqueProductNameForUpdate)
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

        RuleFor(x => x.UpdatedBy)
            .MaximumLength(200)
            .WithMessage("Updated by field cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.UpdatedBy));

        RuleFor(x => x.UpdateReason)
            .MaximumLength(500)
            .WithMessage("Update reason cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.UpdateReason));
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
    /// Validates that the product name is unique (excluding the current product)
    /// </summary>
    /// <param name="command">The update command</param>
    /// <param name="name">Product name to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if name is unique, false otherwise</returns>
    private async Task<bool> BeUniqueProductNameForUpdate(
        UpdateProductCommand command, 
        string name, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true; // Let the NotEmpty rule handle this

        return !await _repository.IsNameTakenAsync(name, command.Id, cancellationToken);
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