using MediatR;
using Products.API.Application.DTOs;
using Products.API.Application.Extensions;
using Products.API.Application.Queries;
using Products.API.Domain.Entities;
using Products.API.Domain.Interfaces;
using System.Linq.Expressions;

namespace Products.API.Application.Handlers;

/// <summary>
/// Query handler for retrieving paginated products with advanced filtering
/// Implements CQRS pattern with MediatR for efficient data retrieval
/// </summary>
/// <param name="repository">Product repository for data access</param>
public class GetProductsPagedQueryHandler(
    IProductRepository repository) : IRequestHandler<GetProductsPagedQuery, PagedProductResult>
{
    /// <summary>
    /// Handles the paginated products query with advanced filtering
    /// </summary>
    /// <param name="request">Paged query with filtering and sorting options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PagedProductResult with products and pagination metadata</returns>
    public async Task<PagedProductResult> Handle(GetProductsPagedQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate pagination parameters
        if (request.PageNumber < 1 || request.PageSize < 1)
        {
            return new PagedProductResult([], 0, request.PageNumber, request.PageSize);
        }

        // For now, use the existing GetPagedAsync method
        // This provides basic pagination without advanced filtering
        var (products, totalCount) = await repository.GetPagedAsync(
            request.PageNumber, 
            request.PageSize, 
            cancellationToken);

        // Apply in-memory filtering if needed (not ideal for large datasets)
        var filteredProducts = ApplyFilters(products, request);

        // Map to DTOs
        var productDtos = filteredProducts.ToDto();

        // Return paginated result
        return new PagedProductResult(
            productDtos,
            totalCount, // Note: this count might be inaccurate with filtering
            request.PageNumber,
            request.PageSize);
    }

    /// <summary>
    /// Applies filtering to the products in memory
    /// Note: This is not ideal for large datasets - database filtering would be better
    /// </summary>
    private static List<Product> ApplyFilters(List<Product> products, GetProductsPagedQuery request)
    {
        var filtered = products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            filtered = filtered.Where(p => p.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (request.MinPrice.HasValue)
        {
            filtered = filtered.Where(p => p.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            filtered = filtered.Where(p => p.Price <= request.MaxPrice.Value);
        }

        if (request.MinStock.HasValue)
        {
            filtered = filtered.Where(p => p.Stock >= request.MinStock.Value);
        }

        if (!request.IncludeDeleted)
        {
            filtered = filtered.Where(p => !p.IsDeleted);
        }

        return filtered.ToList();
    }
}