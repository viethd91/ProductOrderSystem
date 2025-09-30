using MediatR;
using Products.API.Application.DTOs;
using Products.API.Application.Extensions;
using Products.API.Application.Queries;
using Products.API.Domain.Entities;
using Products.API.Domain.Interfaces;

namespace Products.API.Application.Handlers;

/// <summary>
/// Query handler for retrieving all products
/// Implements CQRS pattern with MediatR for read-only operations
/// </summary>
/// <param name="repository">Product repository for data access</param>
/// <param name="mapper">AutoMapper for entity to DTO mapping</param>
public class GetAllProductsQueryHandler(
    IProductRepository repository) : IRequestHandler<GetAllProductsQuery, List<ProductDto>>
{
    /// <summary>
    /// Handles the get all products query
    /// </summary>
    /// <param name="request">Query request with optional filtering parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of ProductDto representing all products</returns>
    public async Task<List<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Determine which repository method to use based on IncludeDeleted flag
        var products = request.IncludeDeleted 
            ? await repository.GetAllAsync(cancellationToken)
            : await repository.GetActiveAsync(cancellationToken);

        // Apply sorting if specified
        if (!string.IsNullOrEmpty(request.SortBy))
        {
            products = ApplySorting(products, request.SortBy, request.SortDirection);
        }

        // Map domain entities to DTOs
        var productDtos = products.Select(p => p.ToDto()).ToList();

        return productDtos;
    }

    /// <summary>
    /// Applies sorting to the products list based on specified criteria
    /// </summary>
    /// <param name="products">List of products to sort</param>
    /// <param name="sortBy">Field to sort by</param>
    /// <param name="sortDirection">Sort direction (asc/desc)</param>
    /// <returns>Sorted list of products</returns>
    private static List<Product> ApplySorting(List<Product> products, string sortBy, string sortDirection)
    {
        var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLower() switch
        {
            "name" => isDescending 
                ? products.OrderByDescending(p => p.Name).ToList()
                : products.OrderBy(p => p.Name).ToList(),
                
            "price" => isDescending
                ? products.OrderByDescending(p => p.Price).ToList()
                : products.OrderBy(p => p.Price).ToList(),
                
            "stock" => isDescending
                ? products.OrderByDescending(p => p.Stock).ToList()
                : products.OrderBy(p => p.Stock).ToList(),
                
            "createdat" => isDescending
                ? products.OrderByDescending(p => p.CreatedAt).ToList()
                : products.OrderBy(p => p.CreatedAt).ToList(),
                
            "updatedat" => isDescending
                ? products.OrderByDescending(p => p.UpdatedAt).ToList()
                : products.OrderBy(p => p.UpdatedAt).ToList(),
                
            _ => products.OrderBy(p => p.Name).ToList() // Default sort by name
        };
    }
}