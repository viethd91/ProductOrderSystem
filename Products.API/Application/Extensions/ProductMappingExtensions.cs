using Products.API.Application.DTOs;
using Products.API.Domain.Entities;

namespace Products.API.Application.Extensions;

/// <summary>
/// Extension methods for manual mapping between Product entity and ProductDto
/// </summary>
public static class ProductMappingExtensions
{
    /// <summary>
    /// Maps Product entity to ProductDto
    /// </summary>
    /// <param name="product">Product entity to map</param>
    /// <returns>ProductDto mapped from entity</returns>
    public static ProductDto ToDto(this Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            IsDeleted = product.IsDeleted,
            IsAvailable = product.IsInStock && !product.IsDeleted
        };
    }

    /// <summary>
    /// Maps a list of Product entities to a list of ProductDtos
    /// </summary>
    /// <param name="products">List of Product entities</param>
    /// <returns>List of ProductDto mapped from entities</returns>
    public static List<ProductDto> ToDto(this IEnumerable<Product> products)
    {
        return products.Select(p => p.ToDto()).ToList();
    }

    // Remove the following method to fix CS0111:
    // public static ProductDto? ToDto(this Product? product)
    // The nullable overload is not needed because the non-nullable version already handles null via ?. operator.
}