using MediatR;
using Products.API.Application.Commands;
using Products.API.Application.DTOs;
using Products.API.Application.Extensions;
using Products.API.Application.Interfaces;
using Products.API.Domain.Entities;
using Products.API.Domain.Interfaces;
using Shared.Messaging; // Use shared interface
using Shared.IntegrationEvents;

namespace Products.API.Application.Handlers;

/// <summary>
/// Command handler for creating new products
/// Implements CQRS pattern with manual mapping, domain event publishing, and integration event publishing
/// </summary>
/// <param name="repository">Product repository for data persistence</param>
/// <param name="messageBus">Message bus for publishing domain (internal) events</param>
/// <param name="eventPublisher">Event publisher for publishing integration events to other bounded contexts</param>
public class CreateProductCommandHandler(
    IProductRepository repository,
    IMessageBus messageBus, // Now uses Shared.Messaging.IMessageBus
    IEventPublisher eventPublisher) : IRequestHandler<CreateProductCommand, ProductDto>
{
    /// <summary>
    /// Handles the create product command
    /// </summary>
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Business rule: unique product name
        var nameExists = await repository.IsNameTakenAsync(request.Name, cancellationToken: cancellationToken);
        if (nameExists)
        {
            throw new InvalidOperationException($"A product with the name '{request.Name}' already exists");
        }

        // Create domain entity
        var product = new Product(
            request.Name,
            request.Price,
            request.Stock
        );

        // Persist
        await repository.AddAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        // Publish domain events (internal to this microservice boundary)
        await PublishDomainEvents(product, cancellationToken);

        // Publish integration event (external contract for other services)
        var integrationEvent = new ProductCreatedIntegrationEvent(
            ProductId: product.Id,
            ProductName: product.Name,
            Price: product.Price,
            Stock: product.Stock
        );

        await eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        // Map to DTO
        return product.ToDto();
    }

    /// <summary>
    /// Publishes all domain events from the product entity
    /// </summary>
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