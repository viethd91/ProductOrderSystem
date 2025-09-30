using FluentValidation;
using Products.API.Application.Commands;
using Products.API.Domain.Interfaces;

namespace Products.API.Application.Validators;

/// <summary>
/// FluentValidation validator for DeleteProductCommand
/// Ensures business rules are enforced for product deletion
/// </summary>
public class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    private readonly IProductRepository _repository;

    /// <summary>
    /// Constructor with repository dependency for async validation
    /// </summary>
    /// <param name="repository">Product repository for business rule validation</param>
    public DeleteProductCommandValidator(IProductRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        ConfigureValidationRules();
    }

    /// <summary>
    /// Configures all validation rules for DeleteProductCommand
    /// </summary>
    private void ConfigureValidationRules()
    {
        // ID validation
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Product ID is required.")
            .MustAsync(ProductMustExist)
            .WithMessage("Product with the specified ID does not exist.")
            .MustAsync(ProductMustNotBeAlreadyDeleted)
            .WithMessage("Product is already deleted.")
            .When(x => !x.HardDelete); // Only check if not hard delete

        // Optional field validations
        RuleFor(x => x.DeletionReason)
            .MaximumLength(500)
            .WithMessage("Deletion reason cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.DeletionReason));

        RuleFor(x => x.DeletedBy)
            .MaximumLength(200)
            .WithMessage("Deleted by field cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.DeletedBy));

        // Business rule: Hard delete should require additional validation
        RuleFor(x => x.HardDelete)
            .Must((command, hardDelete) => !hardDelete || !string.IsNullOrEmpty(command.DeletionReason))
            .WithMessage("Hard delete requires a deletion reason.")
            .When(x => x.HardDelete);
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
    /// Validates that the product is not already deleted (for soft delete only)
    /// </summary>
    /// <param name="id">Product ID to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if product is not deleted, false otherwise</returns>
    private async Task<bool> ProductMustNotBeAlreadyDeleted(Guid id, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        return product != null && !product.IsDeleted;
    }
}