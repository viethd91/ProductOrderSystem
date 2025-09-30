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
/// Command handler for creating new products
/// Implements CQRS pattern with integration event publishing
/// </summary>
/// <param name="repository">Product repository for data persistence</param>
/// <param name="eventPublisher">Event publisher for integration events</param>
public class CreateProductCommandHandler(
    IProductRepository repository,
    IEventPublisher eventPublisher) : IRequestHandler<CreateProductCommand, ProductDto>
{
    /// <summary>
    /// Handles the create product command
    /// </summary>
    /// <param name="request">Create product command with product details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProductDto representing the created product</returns>
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Check for duplicate name
        var nameExists = await repository.IsNameTakenAsync(request.Name, null, cancellationToken);
        if (nameExists)
        {
            throw new InvalidOperationException($"A product with the name '{request.Name}' already exists");
        }

        // Create product using domain constructor
        var product = new Product(request.Name, request.Price, request.Stock);

        // Add to repository
        await repository.AddAsync(product, cancellationToken);
        
        // Save changes
        await repository.SaveChangesAsync(cancellationToken);

        // Publish integration event
        var integrationEvent = new ProductCreatedIntegrationEvent(
            product.Id,
            product.Name,
            product.Price,
            product.Stock,
            DateTime.UtcNow);

        await eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        // Map and return DTO
        var productDto = product.ToDto();
        return productDto;
    }
}