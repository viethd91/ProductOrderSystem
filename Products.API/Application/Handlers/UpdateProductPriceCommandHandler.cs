using MediatR;
using Products.API.Application.Commands;
using Products.API.Application.DTOs;
using Products.API.Application.Extensions;
using Products.API.Domain.Entities;
using Products.API.Domain.Interfaces;
using Shared.Messaging;
using Shared.IntegrationEvents;

namespace Products.API.Application.Handlers;

/// <summary>
/// Specialized command handler for updating product prices
/// Useful for price management scenarios with specific business logic
/// </summary>
/// <param name="repository">Product repository for data persistence</param>
/// <param name="messageBus">Message bus for publishing integration events</param>
public class UpdateProductPriceCommandHandler(
    IProductRepository repository,
    IMessageBus messageBus) : IRequestHandler<UpdateProductPriceCommand, ProductDto>
{
    /// <summary>
    /// Handles the update product price command
    /// </summary>
    /// <param name="request">Update price command with new price details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProductDto representing the updated product</returns>
    public async Task<ProductDto> Handle(UpdateProductPriceCommand request, CancellationToken cancellationToken)
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
            throw new InvalidOperationException($"Cannot update price for deleted product with ID {request.Id}");
        }

        // Store old price for integration event
        var oldPrice = product.Price;

        // Update price using domain method
        product.UpdatePrice(request.NewPrice);

        // Update in repository
        await repository.UpdateAsync(product, cancellationToken);
        
        // Save changes
        await repository.SaveChangesAsync(cancellationToken);

        // Publish integration event if price actually changed
        if (oldPrice != request.NewPrice)
        {
            var integrationEvent = new ProductPriceChangedIntegrationEvent(
                ProductId: product.Id,
                ProductName: product.Name,
                OldPrice: oldPrice,
                NewPrice: request.NewPrice);

            await messageBus.PublishAsync(integrationEvent, cancellationToken);
        }

        // Map and return DTO
        var productDto = product.ToDto();
        return productDto;
    }
}