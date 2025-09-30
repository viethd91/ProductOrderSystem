using MediatR;
using Products.API.Application.DTOs;

namespace Products.API.Application.Queries;

/// <summary>
/// Query for retrieving products with pagination support
/// Useful for efficient data loading in UI grids and lists
/// </summary>
/// <param name="PageNumber">Page number (1-based)</param>
/// <param name="PageSize">Number of items per page</param>
public record GetProductsPagedQuery(
    int PageNumber,
    int PageSize
) : IRequest<PagedProductResult>
{
    /// <summary>
    /// Optional search term for filtering products by name
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Whether to include soft deleted products
    /// Default is false (only active products)
    /// </summary>
    public bool IncludeDeleted { get; init; } = false;

    /// <summary>
    /// Sort field (Name, Price, Stock, CreatedAt, UpdatedAt)
    /// Default is "Name"
    /// </summary>
    public string SortBy { get; init; } = "Name";

    /// <summary>
    /// Sort direction (asc, desc)
    /// Default is ascending
    /// </summary>
    public string SortDirection { get; init; } = "asc";

    /// <summary>
    /// Optional price range filter - minimum price
    /// </summary>
    public decimal? MinPrice { get; init; }

    /// <summary>
    /// Optional price range filter - maximum price
    /// </summary>
    public decimal? MaxPrice { get; init; }

    /// <summary>
    /// Optional stock level filter - only products with stock above this value
    /// </summary>
    public int? MinStock { get; init; }

    /// <summary>
    /// User requesting the paged results (for audit/logging purposes)
    /// </summary>
    public string? RequestedBy { get; init; }
}

/// <summary>
/// Result object for paged product queries
/// Contains both the data and pagination metadata
/// </summary>
/// <param name="Products">List of products for the current page</param>
/// <param name="TotalCount">Total number of products matching the criteria</param>
/// <param name="PageNumber">Current page number</param>
/// <param name="PageSize">Number of items per page</param>
public record PagedProductResult(
    List<ProductDto> Products,
    int TotalCount,
    int PageNumber,
    int PageSize
)
{
    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPrevious => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNext => PageNumber < TotalPages;

    /// <summary>
    /// Index of first item on current page (1-based)
    /// </summary>
    public int FirstItemIndex => TotalCount == 0 ? 0 : (PageNumber - 1) * PageSize + 1;

    /// <summary>
    /// Index of last item on current page (1-based)
    /// </summary>
    public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalCount);
}