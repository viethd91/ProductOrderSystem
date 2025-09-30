using Products.API.Domain.Entities;

namespace Products.API.Domain.Interfaces;

/// <summary>
/// Repository interface for Product aggregate
/// Defines the contract for Product data access operations
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Gets a product by its unique identifier
    /// </summary>
    /// <param name="id">The product unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The product if found, otherwise null</returns>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all products (active and deleted based on includeDeleted parameter)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all products</returns>
    Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets only active products (not soft deleted)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active products</returns>
    Task<List<Product>> GetActiveAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new product to the repository
    /// </summary>
    /// <param name="product">The product to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing product
    /// Note: In DDD, this typically just marks the entity as modified
    /// The actual update happens when SaveChangesAsync is called
    /// </summary>
    /// <param name="product">The product to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs soft delete of a product by ID
    /// </summary>
    /// <param name="id">The product ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if product was found and deleted, false otherwise</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Hard delete of a product (removes from database completely)
    /// Use with caution - typically only for testing or admin operations
    /// </summary>
    /// <param name="id">The product ID to remove permanently</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if product was found and removed, false otherwise</returns>
    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves all changes to the underlying data store
    /// This is where domain events are typically published
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of affected rows</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a product exists with the given ID
    /// </summary>
    /// <param name="id">The product ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if product exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets products by name (partial match, case-insensitive)
    /// Useful for search functionality
    /// </summary>
    /// <param name="name">The product name to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products matching the name criteria</returns>
    Task<List<Product>> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets products with low stock (below the specified threshold)
    /// Useful for inventory management
    /// </summary>
    /// <param name="threshold">The stock threshold (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products with low stock</returns>
    Task<List<Product>> GetLowStockProductsAsync(int threshold = 10, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets products by price range
    /// </summary>
    /// <param name="minPrice">Minimum price (inclusive)</param>
    /// <param name="maxPrice">Maximum price (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products within the price range</returns>
    Task<List<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets paginated products for efficient data retrieval
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of products</returns>
    Task<(List<Product> Products, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a product with the given name already exists (for uniqueness validation)
    /// </summary>
    /// <param name="name">Product name to check</param>
    /// <param name="excludeId">Product ID to exclude from the check (useful for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a product with the name exists, false otherwise</returns>
    Task<bool> IsNameTakenAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}