using MediatR;
using Products.API.Application.DTOs;
using Products.API.Application.Extensions;
using Products.API.Application.Queries;
using Products.API.Domain.Interfaces;

namespace Products.API.Application.Handlers;

/// <summary>
/// Query handler for retrieving a single product by ID
/// Implements CQRS pattern with MediatR for read-only operations
/// </summary>
/// <param name="repository">Product repository for data access</param>
public class GetProductByIdQueryHandler(
    IProductRepository repository) : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    /// <summary>
    /// Handles the get product by ID query
    /// </summary>
    /// <param name="request">Query request with product ID and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProductDto if found, otherwise null</returns>
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Get product by ID
        var product = await repository.GetByIdAsync(request.Id, cancellationToken);

        // If product not found, return null
        if (product == null)
        {
            return null;
        }

        // Check if we should include deleted products
        if (product.IsDeleted && !request.IncludeDeleted)
        {
            return null;
        }

        // Map domain entity to DTO using extension method
        return product.ToDto();
    }
}