using Microsoft.EntityFrameworkCore;
using Products.API.Domain.Entities;
using Products.API.Domain.Interfaces;
using Products.API.Infrastructure.Data;

namespace Products.API.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of IProductRepository
/// Handles Product aggregate persistence operations
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ProductContext _context;

    /// <summary>
    /// Constructor with ProductContext dependency injection
    /// </summary>
    /// <param name="context">Product database context</param>
    public ProductRepository(ProductContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Gets a product by its unique identifier
    /// </summary>
    /// <param name="id">The product unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The product if found, otherwise null</returns>
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets all products including soft deleted ones (ignores global query filter)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all products</returns>
    public async Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .IgnoreQueryFilters() // Include soft deleted products
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets only active products (not soft deleted) - uses global query filter
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active products</returns>
    public async Task<List<Product>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a new product to the repository
    /// </summary>
    /// <param name="product">The product to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        await _context.Products.AddAsync(product, cancellationToken);
    }

    /// <summary>
    /// Updates an existing product (marks entity as modified)
    /// </summary>
    /// <param name="product">The product to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        // Check if entity is already being tracked
        var existingEntry = _context.Entry(product);
        if (existingEntry.State == EntityState.Detached)
        {
            // If not tracked, attach and mark as modified
            _context.Products.Update(product);
        }
        else
        {
            // If already tracked, just mark as modified
            existingEntry.State = EntityState.Modified;
        }

        await Task.CompletedTask; // Make method async for interface compatibility
    }

    /// <summary>
    /// Performs soft delete of a product by ID
    /// </summary>
    /// <param name="id">The product ID to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if product was found and deleted, false otherwise</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
            return false;

        // Use domain method for soft delete (raises domain events)
        product.Delete("Soft deleted via repository", "System");
        
        return true;
    }

    /// <summary>
    /// Hard delete of a product (removes from database completely)
    /// </summary>
    /// <param name="id">The product ID to remove permanently</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if product was found and removed, false otherwise</returns>
    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .IgnoreQueryFilters() // Include soft deleted products for hard delete
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
            return false;

        _context.Products.Remove(product);
        return true;
    }

    /// <summary>
    /// Saves all changes to the underlying data store
    /// This triggers domain event publishing via ProductContext
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of affected rows</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a product exists with the given ID
    /// </summary>
    /// <param name="id">The product ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if product exists, false otherwise</returns>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets products by name (partial match, case-insensitive)
    /// </summary>
    /// <param name="name">The product name to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products matching the name criteria</returns>
    public async Task<List<Product>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return [];

        return await _context.Products
            .Where(p => EF.Functions.Like(p.Name, $"%{name}%"))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets products with low stock (below the specified threshold)
    /// </summary>
    /// <param name="threshold">The stock threshold (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products with low stock</returns>
    public async Task<List<Product>> GetLowStockProductsAsync(int threshold = 10, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.Stock < threshold)
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets products by price range
    /// </summary>
    /// <param name="minPrice">Minimum price (inclusive)</param>
    /// <param name="maxPrice">Maximum price (inclusive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products within the price range</returns>
    public async Task<List<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default)
    {
        if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
            return [];

        return await _context.Products
            .Where(p => p.Price >= minPrice && p.Price <= maxPrice)
            .OrderBy(p => p.Price)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets paginated products for efficient data retrieval
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of products with total count</returns>
    public async Task<(List<Product> Products, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1)
            return ([], 0);

        var query = _context.Products.AsQueryable();

        var totalCount = await query.CountAsync(cancellationToken);

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (products, totalCount);
    }

    /// <summary>
    /// Checks if a product with the given name already exists (for uniqueness validation)
    /// </summary>
    /// <param name="name">Product name to check</param>
    /// <param name="excludeId">Product ID to exclude from the check (useful for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a product with the name exists, false otherwise</returns>
    public async Task<bool> IsNameTakenAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var query = _context.Products
            .Where(p => p.Name.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Gets products with advanced filtering and sorting
    /// Useful for complex queries from the application layer
    /// </summary>
    /// <param name="filter">Optional filter predicate</param>
    /// <param name="orderBy">Optional ordering function</param>
    /// <param name="includeDeleted">Whether to include soft deleted products</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Filtered and ordered list of products</returns>
    public async Task<List<Product>> GetWithFilterAsync(
        System.Linq.Expressions.Expression<Func<Product, bool>>? filter = null,
        Func<IQueryable<Product>, IOrderedQueryable<Product>>? orderBy = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _context.Products;

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }
        else
        {
            query = query.OrderBy(p => p.Name);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Bulk update products (for performance when updating many products)
    /// Note: This bypasses domain events - use carefully
    /// </summary>
    /// <param name="productIds">List of product IDs to update</param>
    /// <param name="updateAction">Action to perform on each product</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of updated products</returns>
    public async Task<int> BulkUpdateAsync(
        IEnumerable<Guid> productIds, 
        Action<Product> updateAction, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateAction);

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            updateAction(product);
        }

        return products.Count;
    }
}