using MediatR;
using Products.API.Application.Commands;
using Products.API.Application.Interfaces;
using Products.API.Domain.Entities;
using Products.API.Domain.Interfaces;

namespace Products.API.Application.Handlers;

/// <summary>
/// Command handler for deleting products (soft or hard delete)
/// Implements CQRS pattern
/// </summary>
/// <param name="repository">Product repository for data persistence</param>
public class DeleteProductCommandHandler(
    IProductRepository repository) : IRequestHandler<DeleteProductCommand, bool>
{
    /// <summary>
    /// Handles the delete product command
    /// </summary>
    /// <param name="request">Delete product command with deletion details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if product was successfully deleted, false if not found</returns>
    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Retrieve existing product
        var product = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            return false; // Product not found
        }

        bool result;

        if (request.HardDelete)
        {
            // Perform hard delete (completely remove from database)
            result = await repository.RemoveAsync(request.Id, cancellationToken);
        }
        else
        {
            // Perform soft delete using domain method
            if (product.IsDeleted)
            {
                return false; // Already deleted
            }

            product.Delete(request.DeletionReason, request.DeletedBy);
            await repository.UpdateAsync(product, cancellationToken);
            result = true;
        }

        if (result)
        {
            // Save changes
            await repository.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}