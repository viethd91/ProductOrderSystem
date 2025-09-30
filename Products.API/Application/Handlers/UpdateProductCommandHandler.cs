using MediatR;
using Products.API.Application.Commands;
using Products.API.Application.DTOs;
using Products.API.Application.Extensions;
using Products.API.Application.Interfaces;
using Products.API.Domain.Entities;
using Products.API.Domain.Interfaces;
using Shared.IntegrationEvents;

namespace Products.API.Application.Handlers;

/// <summary>
/// Command handler for updating existing products
/// Implements CQRS pattern with integration event publishing for price changes
/// </summary>
/// <param name="repository">Product repository for data persistence</param>
/// <param name="eventPublisher">Event publisher for integration events</param>
public class UpdateProductCommandHandler(
    IProductRepository repository,
    IEventPublisher eventPublisher) : IRequestHandler<UpdateProductCommand, ProductDto>
{
    /// <summary>
    /// Handles the update product command
    /// </summary>
    /// <param name="request">Update product command with new product details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProductDto representing the updated product</returns>
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Retrieve existing product
        var product = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
        {
            throw new InvalidOperationException($"Product with ID {request.Id} not found");
        }

        // Check if product is deleted
        if (product.IsDeleted)
        {
            throw new InvalidOperationException($"Cannot update deleted product with ID {request.Id}");
        }

        // Track old price for integration event
        var oldPrice = product.Price;

        // Check name uniqueness if name is changing
        if (!string.Equals(product.Name, request.Name, StringComparison.Ordinal))
        {
            var nameExists = await repository.IsNameTakenAsync(request.Name, request.Id, cancellationToken);
            if (nameExists)
            {
                throw new InvalidOperationException($"A product with the name '{request.Name}' already exists");
            }
        }

        // Update product properties using domain methods
        product.UpdateName(request.Name);
        product.UpdatePrice(request.Price);
        product.UpdateStock(request.Stock);

        // Persist changes
        await repository.UpdateAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        // Publish integration event only if price changed
        if (oldPrice != product.Price)
        {
            var integrationEvent = new ProductPriceChangedIntegrationEvent(
                ProductId: product.Id,
                ProductName: product.Name,
                OldPrice: oldPrice,
                NewPrice: product.Price,
                ChangedAtUtc: DateTime.UtcNow
            );

            await eventPublisher.PublishAsync(integrationEvent, cancellationToken);
        }

        // Map and return DTO
        return product.ToDto();
    }
}