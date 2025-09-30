
using MediatR;
using Products.API.Application.DTOs;
using Products.API.Application.Extensions;
using Products.API.Application.Queries;
using Products.API.Domain.Interfaces;

namespace Products.API.Application.Handlers;

/// <summary>
/// Query handler for searching products by name
/// Implements CQRS pattern with MediatR for read-only search operations
/// </summary>
/// <param name="repository">Product repository for data access</param>
/// <param name="mapper">AutoMapper for entity to DTO mapping</param>
public class GetProductsByNameQueryHandler(
    IProductRepository repository) : IRequestHandler<GetProductsByNameQuery, List<ProductDto>>
{
    /// <summary>
    /// Handles the search products by name query
    /// </summary>
    /// <param name="request">Search query with name criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of ProductDto matching the search criteria</returns>
    public async Task<List<ProductDto>> Handle(GetProductsByNameQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate search term
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return [];
        }

        // Search products by name using repository method
        var products = await repository.GetByNameAsync(request.SearchTerm, cancellationToken);

        // Filter out deleted products if not explicitly requested
        if (!request.IncludeDeleted)
        {
            products = products.Where(p => !p.IsDeleted).ToList();
        }

        // Apply max results limit
        if (request.MaxResults > 0 && products.Count > request.MaxResults)
        {
            products = products.Take(request.MaxResults).ToList();
        }

        // Map domain entities to DTOs
        var productDtos = products.Select(p => p.ToDto()).ToList();

        return productDtos;
    }
}