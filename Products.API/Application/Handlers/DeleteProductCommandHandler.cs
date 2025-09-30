using MediatR;
using Products.API.Application.Commands;
using Products.API.Application.Interfaces;
using Products.API.Domain.Entities;
using Products.API.Domain.Interfaces;
using Shared.Messaging; // Use shared interface instead of local one

namespace Products.API.Application.Handlers;

/// <summary>
/// Command handler for deleting products (soft or hard delete)
/// Implements CQRS pattern with domain event publishing
/// </summary>
/// <param name="repository">Product repository for data persistence</param>
/// <param name="messageBus">Message bus for publishing domain events</param>
public class DeleteProductCommandHandler(
    IProductRepository repository,
    IMessageBus messageBus) : IRequestHandler<DeleteProductCommand, bool>
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

            // Publish domain events (only for soft delete, hard delete doesn't generate events)
            if (!request.HardDelete)
            {
                await PublishDomainEvents(product, cancellationToken);
            }
        }

        return result;
    }

    /// <summary>
    /// Publishes all domain events from the product entity
    /// </summary>
    /// <param name="product">Product entity with domain events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task PublishDomainEvents(Product product, CancellationToken cancellationToken)
    {
        var domainEvents = product.DomainEvents.ToList();
        
        foreach (var domainEvent in domainEvents)
        {
            await messageBus.PublishAsync(domainEvent, cancellationToken);
        }

        product.ClearDomainEvents();
    }
}