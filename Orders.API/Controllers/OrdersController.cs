using MediatR;
using Microsoft.AspNetCore.Mvc;
using Orders.API.Application.Commands;
using Orders.API.Application.DTOs;
using Orders.API.Application.Queries;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Enums;

namespace Orders.API.Controllers;

/// <summary>
/// RESTful API controller for order management operations
/// Implements CQRS pattern using MediatR for command and query separation
/// </summary>
/// <param name="mediator">MediatR instance for handling commands and queries</param>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a new order with the specified items and customer information
    /// </summary>
    /// <param name="command">Order creation command with customer and item details</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Created order information</returns>
    /// <response code="201">Order created successfully</response>
    /// <response code="400">Invalid order data provided</response>
    /// <response code="422">Business validation failed</response>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> CreateOrder(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var orderDto = await mediator.Send(command, cancellationToken);
            
            return CreatedAtAction(
                nameof(GetOrderById),
                new { id = orderDto.Id },
                orderDto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Order Data",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Business Rule Violation",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }
    }

    /// <summary>
    /// Retrieves orders based on optional filtering criteria
    /// Supports filtering by customer ID, status, or retrieves all orders
    /// </summary>
    /// <param name="customerId">Optional customer ID to filter orders</param>
    /// <param name="status">Optional order status to filter orders</param>
    /// <param name="includeCancelled">Whether to include cancelled orders (default: false)</param>
    /// <param name="maxResults">Maximum number of results to return (default: no limit)</param>
    /// <param name="sortBy">Field to sort by (CreatedAt, OrderDate, TotalAmount, CustomerName)</param>
    /// <param name="sortDirection">Sort direction (asc, desc)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>List of orders matching the criteria</returns>
    /// <response code="200">Orders retrieved successfully</response>
    /// <response code="400">Invalid query parameters</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<OrderDto>>> GetOrders(
        [FromQuery] Guid? customerId = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] bool includeCancelled = false,
        [FromQuery] int maxResults = 0,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Determine which query to use based on provided parameters
            if (customerId.HasValue)
            {
                var customerQuery = new GetOrdersByCustomerIdQuery(customerId.Value)
                {
                    IncludeCancelled = includeCancelled,
                    MaxResults = maxResults,
                    SortBy = sortBy,
                    SortDirection = sortDirection,
                    RequestedBy = GetCurrentUser()
                };
                
                return Ok(await mediator.Send(customerQuery, cancellationToken));
            }
            
            if (status.HasValue)
            {
                var statusQuery = new GetOrdersByStatusQuery(status.Value)
                {
                    MaxResults = maxResults,
                    SortBy = sortBy,
                    SortDirection = sortDirection,
                    RequestedBy = GetCurrentUser()
                };
                
                return Ok(await mediator.Send(statusQuery, cancellationToken));
            }
            
            // Default to getting all orders
            var allOrdersQuery = new GetAllOrdersQuery
            {
                IncludeCancelled = includeCancelled,
                MaxResults = maxResults,
                SortBy = sortBy,
                SortDirection = sortDirection,
                RequestedBy = GetCurrentUser()
            };
            
            return Ok(await mediator.Send(allOrdersQuery, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Query Parameters",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Retrieves a specific order by its unique identifier
    /// Includes all order items and complete order details
    /// </summary>
    /// <param name="id">Unique order identifier</param>
    /// <param name="includeCancelled">Whether to include the order if it's cancelled</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Order details if found</returns>
    /// <response code="200">Order found and returned successfully</response>
    /// <response code="404">Order not found</response>
    /// <response code="400">Invalid order ID format</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> GetOrderById(
        [FromRoute] Guid id,
        [FromQuery] bool includeCancelled = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Order ID",
                    Detail = "Order ID cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var query = new GetOrderByIdQuery(id)
            {
                IncludeCancelled = includeCancelled,
                RequestedBy = GetCurrentUser()
            };

            var orderDto = await mediator.Send(query, cancellationToken);
            
            if (orderDto == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Order Not Found",
                    Detail = $"Order with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Error Retrieving Order",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Updates the status of an existing order
    /// Enforces business rules for valid status transitions
    /// </summary>
    /// <param name="id">Unique order identifier</param>
    /// <param name="request">Status update request with new status and optional metadata</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated order information</returns>
    /// <response code="200">Order status updated successfully</response>
    /// <response code="400">Invalid status transition or request data</response>
    /// <response code="404">Order not found</response>
    /// <response code="422">Business rule violation</response>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Order ID",
                    Detail = "Order ID cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (!Enum.TryParse<OrderStatus>(request.NewStatus, true, out var newStatus))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Order Status",
                    Detail = $"'{request.NewStatus}' is not a valid order status",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var command = new UpdateOrderStatusCommand(id, newStatus)
            {
                StatusChangeReason = request.Reason,
                UpdatedBy = GetCurrentUser(),
                TrackingNumber = request.TrackingNumber,
                EstimatedDeliveryDate = request.EstimatedDeliveryDate,
                Notes = request.Notes
            };

            var orderDto = await mediator.Send(command, cancellationToken);
            return Ok(orderDto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Order Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Invalid Status Transition",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request Data",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Cancels an existing order with a specified reason
    /// Only orders in Pending or Confirmed status can be cancelled
    /// </summary>
    /// <param name="id">Unique order identifier</param>
    /// <param name="request">Cancellation request with reason and optional metadata</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Boolean indicating cancellation success</returns>
    /// <response code="200">Order cancellation processed (true if cancelled, false if not possible)</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Order not found</response>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(CancelOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CancelOrderResponse>> CancelOrder(
        [FromRoute] Guid id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Order ID",
                    Detail = "Order ID cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Missing Cancellation Reason",
                    Detail = "Cancellation reason is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var command = new CancelOrderCommand(id, request.Reason)
            {
                CancelledBy = GetCurrentUser(),
                CancellationDetails = request.Details,
                NotifyCustomer = request.NotifyCustomer,
                ProcessRefundImmediately = request.ProcessRefund
            };

            var result = await mediator.Send(command, cancellationToken);
            
            return Ok(new CancelOrderResponse
            {
                Success = result,
                Message = result 
                    ? "Order cancelled successfully" 
                    : "Order cannot be cancelled in its current state"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request Data",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Adds a new item to an existing order
    /// Only works for orders in Pending status
    /// </summary>
    /// <param name="id">Unique order identifier</param>
    /// <param name="request">Request containing item details to add</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Updated order information with new item</returns>
    /// <response code="200">Item added successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Order not found</response>
    /// <response code="422">Cannot modify order in current status</response>
    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> AddOrderItem(
        [FromRoute] Guid id,
        [FromBody] AddOrderItemRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Order ID",
                    Detail = "Order ID cannot be empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var command = new AddOrderItemCommand(
                id,
                request.ProductId,
                request.ProductName,
                request.Quantity,
                request.UnitPrice)
            {
                AddedBy = GetCurrentUser(),
                ItemNotes = request.ItemNotes,
                AdditionReason = request.AdditionReason,
                MergeIfExists = request.MergeIfExists
            };

            var orderDto = await mediator.Send(command, cancellationToken);
            return Ok(orderDto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Order Not Found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Cannot Modify Order",
                Detail = ex.Message,
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request Data",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    /// <summary>
    /// Gets the current user for audit purposes
    /// In a real application, this would extract from JWT token or authentication context
    /// </summary>
    /// <returns>Current user identifier</returns>
    private string GetCurrentUser()
    {
        // In a real application, extract from HttpContext.User or JWT token
        return User?.Identity?.Name ?? "Anonymous";
    }
}

/// <summary>
/// Request model for updating order status
/// </summary>
public record UpdateOrderStatusRequest
{
    /// <summary>
    /// New status for the order (Pending, Confirmed, Shipped, Delivered, Cancelled)
    /// </summary>
    public string NewStatus { get; init; } = string.Empty;

    /// <summary>
    /// Reason for the status change (required for cancellations)
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Tracking number (required for shipped status)
    /// </summary>
    public string? TrackingNumber { get; init; }

    /// <summary>
    /// Estimated delivery date (optional for shipped status)
    /// </summary>
    public DateTime? EstimatedDeliveryDate { get; init; }

    /// <summary>
    /// Additional notes for the status change
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Request model for cancelling an order
/// </summary>
public record CancelOrderRequest
{
    /// <summary>
    /// Reason for cancellation (required)
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Additional details about the cancellation
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Whether to notify the customer (default: true)
    /// </summary>
    public bool NotifyCustomer { get; init; } = true;

    /// <summary>
    /// Whether to process refund immediately (default: false)
    /// </summary>
    public bool ProcessRefund { get; init; } = false;
}

/// <summary>
/// Response model for order cancellation
/// </summary>
public record CancelOrderResponse
{
    /// <summary>
    /// Whether the cancellation was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Request model for adding an item to an order
/// </summary>
public record AddOrderItemRequest
{
    /// <summary>
    /// Product identifier from catalog
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// Product name for display purposes
    /// </summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>
    /// Quantity to add
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Unit price at time of addition
    /// </summary>
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// Optional notes for this item
    /// </summary>
    public string? ItemNotes { get; init; }

    /// <summary>
    /// Reason for adding the item
    /// </summary>
    public string? AdditionReason { get; init; }

    /// <summary>
    /// Whether to merge with existing item if product already exists (default: true)
    /// </summary>
    public bool MergeIfExists { get; init; } = true;
}