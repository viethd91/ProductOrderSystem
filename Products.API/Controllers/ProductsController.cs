using MediatR;
using Microsoft.AspNetCore.Mvc;
using Products.API.Application.Commands;
using Products.API.Application.DTOs;
using Products.API.Application.Queries;

namespace Products.API.Controllers;

/// <summary>
/// Products API Controller
/// Handles product catalog management operations using CQRS pattern
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    /// <param name="mediator">MediatR mediator for CQRS operations</param>
    /// <param name="logger">Logger for diagnostics and monitoring</param>
    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="command">Product creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product with generated ID</returns>
    /// <response code="201">Product created successfully</response>
    /// <response code="400">Invalid product data provided</response>
    /// <response code="409">Product with the same name already exists</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new product with name: {ProductName}", command?.Name);

            var result = await _mediator.Send(command!, cancellationToken);

            _logger.LogInformation("Successfully created product with ID: {ProductId}", result.Id);

            return CreatedAtAction(
                nameof(GetProductById),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning("Product creation failed - duplicate name: {ProductName}", command?.Name);
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Product creation failed - invalid data: {Error}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all products with optional filtering and sorting
    /// </summary>
    /// <param name="includeDeleted">Include soft-deleted products (default: false)</param>
    /// <param name="sortBy">Sort field: Name, Price, Stock, CreatedAt (default: Name)</param>
    /// <param name="sortDirection">Sort direction: asc, desc (default: asc)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all products</returns>
    /// <response code="200">Products retrieved successfully</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] string sortBy = "Name",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all products - IncludeDeleted: {IncludeDeleted}, SortBy: {SortBy}",
            includeDeleted, sortBy);

        var query = new GetAllProductsQuery
        {
            IncludeDeleted = includeDeleted,
            SortBy = sortBy,
            SortDirection = sortDirection,
            RequestedBy = User.Identity?.Name
        };

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation("Successfully retrieved {ProductCount} products", result.Count);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves a specific product by its unique identifier
    /// </summary>
    /// <param name="id">Product unique identifier</param>
    /// <param name="includeDeleted">Include soft-deleted products (default: false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details if found</returns>
    /// <response code="200">Product found and returned</response>
    /// <response code="404">Product not found</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductById(
        Guid id,
        [FromQuery] bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving product with ID: {ProductId}", id);

        var query = new GetProductByIdQuery(id)
        {
            IncludeDeleted = includeDeleted,
            RequestedBy = User.Identity?.Name
        };

        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            _logger.LogWarning("Product not found with ID: {ProductId}", id);
            return NotFound(new { message = $"Product with ID {id} not found" });
        }

        _logger.LogInformation("Successfully retrieved product: {ProductName} (ID: {ProductId})",
            result.Name, result.Id);

        return Ok(result);
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="id">Product unique identifier</param>
    /// <param name="command">Updated product details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product details</returns>
    /// <response code="200">Product updated successfully</response>
    /// <response code="400">Invalid product data provided</response>
    /// <response code="404">Product not found</response>
    /// <response code="409">Product name already exists</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        // Ensure the ID in the route matches the command
        if (command.Id != id)
        {
            _logger.LogWarning("ID mismatch - Route ID: {RouteId}, Command ID: {CommandId}", id, command.Id);
            return BadRequest(new { message = "Product ID in route must match ID in request body" });
        }

        try
        {
            _logger.LogInformation("Updating product with ID: {ProductId}", id);

            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Successfully updated product: {ProductName} (ID: {ProductId})",
                result.Name, result.Id);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("Product update failed - not found: {ProductId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning("Product update failed - duplicate name: {ProductName}", command?.Name);
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("deleted"))
        {
            _logger.LogWarning("Product update failed - product is deleted: {ProductId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Product update failed - invalid data: {Error}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a product (soft delete by default)
    /// </summary>
    /// <param name="id">Product unique identifier</param>
    /// <param name="hardDelete">Perform hard delete (completely remove from database)</param>
    /// <param name="reason">Reason for deletion (audit purposes)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Product deleted successfully</response>
    /// <response code="404">Product not found</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteProduct(
        Guid id,
        [FromQuery] bool hardDelete = false,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId} (HardDelete: {HardDelete})",
            id, hardDelete);

        var command = new DeleteProductCommand(id)
        {
            HardDelete = hardDelete,
            DeletionReason = reason,
            DeletedBy = User.Identity?.Name
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            _logger.LogWarning("Product deletion failed - not found: {ProductId}", id);
            return NotFound(new { message = $"Product with ID {id} not found" });
        }

        _logger.LogInformation("Successfully deleted product with ID: {ProductId}", id);

        return NoContent();
    }

    /// <summary>
    /// Searches products by name
    /// </summary>
    /// <param name="searchTerm">Search term for product name</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products matching search criteria</returns>
    /// <response code="200">Products found and returned</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProducts(
        [FromQuery] string searchTerm,
        [FromQuery] int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching products with term: {SearchTerm}", searchTerm);

        var query = new GetProductsByNameQuery(searchTerm)
        {
            MaxResults = maxResults,
            RequestedBy = User.Identity?.Name
        };

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation("Search returned {ProductCount} products", result.Count);

        return Ok(result);
    }

    /// <summary>
    /// Gets products with low stock levels
    /// </summary>
    /// <param name="threshold">Stock threshold (default: 10)</param>
    /// <param name="includeOutOfStock">Include out-of-stock products</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products with low stock</returns>
    /// <response code="200">Low stock products retrieved successfully</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetLowStockProducts(
        [FromQuery] int threshold = 10,
        [FromQuery] bool includeOutOfStock = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving low stock products with threshold: {Threshold}", threshold);

        var query = new GetLowStockProductsQuery
        {
            Threshold = threshold,
            IncludeOutOfStock = includeOutOfStock,
            RequestedBy = User.Identity?.Name
        };

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation("Found {ProductCount} products with low stock", result.Count);

        return Ok(result);
    }

    /// <summary>
    /// Updates only the price of a product
    /// </summary>
    /// <param name="id">Product unique identifier</param>
    /// <param name="command">Price update details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated product details</returns>
    /// <response code="200">Price updated successfully</response>
    /// <response code="400">Invalid price data provided</response>
    /// <response code="404">Product not found</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpPatch("{id:guid}/price")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UpdateProductPrice(
        Guid id,
        [FromBody] UpdateProductPriceCommand command,
        CancellationToken cancellationToken = default)
    {
        // Ensure the ID in the route matches the command
        if (command.Id != id)
        {
            return BadRequest(new { message = "Product ID in route must match ID in request body" });
        }

        try
        {
            _logger.LogInformation("Updating price for product ID: {ProductId} to {NewPrice}",
                id, command.NewPrice);

            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation("Successfully updated price for product: {ProductName} (ID: {ProductId})",
                result.Name, result.Id);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("Price update failed - product not found: {ProductId}", id);
            return NotFound(new { message = ex.Message });
        }
    }
}