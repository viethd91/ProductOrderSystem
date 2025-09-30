using Orders.API.Domain.Enums;
using Orders.API.Domain.Interfaces;

namespace Orders.API.Domain.Entities;

/// <summary>
/// Order aggregate root representing a customer order
/// Implements Domain-Driven Design patterns with business rules and domain events
/// </summary>
public class Order
{
    private readonly List<OrderItem> _orderItems = [];
    private decimal _totalAmount;

    /// <summary>
    /// Unique identifier for the order
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Unique order number in format ORD-{timestamp}
    /// </summary>
    public string OrderNumber { get; private set; }

    /// <summary>
    /// Customer identifier who placed the order
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Customer name for display purposes
    /// </summary>
    public string CustomerName { get; private set; }

    /// <summary>
    /// Date and time when the order was placed
    /// </summary>
    public DateTime OrderDate { get; private set; }

    /// <summary>
    /// Current status of the order in its lifecycle
    /// </summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// Total amount calculated from all order items
    /// </summary>
    public decimal TotalAmount => _totalAmount;

    /// <summary>
    /// Collection of items in this order
    /// </summary>
    public IReadOnlyList<OrderItem> OrderItems => _orderItems.AsReadOnly();

    /// <summary>
    /// Date and time when the order was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Date and time when the order was last updated
    /// </summary>
    public DateTime UpdatedAt { get; private set; }


    /// <summary>
    /// Primary constructor for creating a new order
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <param name="customerName">Customer name</param>
    public Order(Guid customerId, string customerName)
    {
        ValidateCustomerId(customerId);
        ValidateCustomerName(customerName);

        Id = Guid.NewGuid();
        OrderNumber = GenerateOrderNumber();
        CustomerId = customerId;
        CustomerName = customerName;
        OrderDate = DateTime.UtcNow;
        Status = OrderStatus.Pending;
        _totalAmount = 0m;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

    }

    /// <summary>
    /// Parameterless constructor for EF Core
    /// </summary>
    private Order()
    {
        OrderNumber = string.Empty;
        CustomerName = string.Empty;
    }

    /// <summary>
    /// Adds an order item to the order
    /// Automatically recalculates the total amount
    /// </summary>
    /// <param name="item">Order item to add</param>
    /// <exception cref="ArgumentNullException">Thrown when item is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when order status doesn't allow modifications</exception>
    public void AddOrderItem(OrderItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot add items to order with status {Status}");
        }

        // Check if item with same product already exists
        var existingItem = _orderItems.FirstOrDefault(oi => oi.ProductId == item.ProductId);
        if (existingItem != null)
        {
            // Update quantity and price of existing item
            existingItem.UpdateQuantity(existingItem.Quantity + item.Quantity);
            existingItem.UpdateUnitPrice(item.UnitPrice); // Use latest price
        }
        else
        {
            _orderItems.Add(item);
        }

        RecalculateTotal();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes an order item by its identifier
    /// Automatically recalculates the total amount
    /// </summary>
    /// <param name="itemId">Order item identifier to remove</param>
    /// <exception cref="InvalidOperationException">Thrown when order status doesn't allow modifications or item not found</exception>
    public void RemoveOrderItem(Guid itemId)
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot remove items from order with status {Status}");
        }

        var item = _orderItems.FirstOrDefault(oi => oi.Id == itemId);
        if (item == null)
        {
            throw new InvalidOperationException($"Order item with ID {itemId} not found");
        }

        _orderItems.Remove(item);
        RecalculateTotal();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the order status with business rule validation
    /// Ensures only valid status transitions are allowed
    /// </summary>
    /// <param name="newStatus">New order status</param>
    /// <exception cref="InvalidOperationException">Thrown when status transition is invalid</exception>
    public void UpdateStatus(OrderStatus newStatus)
    {
        if (Status == newStatus)
            return; // No change needed

        ValidateStatusTransition(Status, newStatus);

        var oldStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;

    }

    /// <summary>
    /// Calculates the total amount from all order items
    /// </summary>
    /// <returns>Total amount of the order</returns>
    public decimal CalculateTotalAmount()
    {
        return _orderItems.Sum(item => item.TotalPrice);
    }

    /// <summary>
    /// Confirms the order by changing status to Confirmed
    /// Validates that order has items before confirming
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when order cannot be confirmed</exception>
    public void Confirm()
    {
        if (!_orderItems.Any())
        {
            throw new InvalidOperationException("Cannot confirm order without items");
        }

        if (_totalAmount <= 0)
        {
            throw new InvalidOperationException("Cannot confirm order with zero or negative total amount");
        }

        UpdateStatus(OrderStatus.Confirmed);
    }

    /// <summary>
    /// Cancels the order by changing status to Cancelled
    /// Only allowed for Pending or Confirmed orders
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when order cannot be cancelled</exception>
    public void Cancel()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException($"Cannot cancel order that has been {Status.ToString().ToLower()}");
        }

        UpdateStatus(OrderStatus.Cancelled);
    }

    /// <summary>
    /// Ships the order by changing status to Shipped
    /// </summary>
    public void Ship()
    {
        UpdateStatus(OrderStatus.Shipped);
    }

    /// <summary>
    /// Marks the order as delivered
    /// </summary>
    public void Deliver()
    {
        UpdateStatus(OrderStatus.Delivered);
    }

    /// <summary>
    /// Checks if the order can be modified (add/remove items)
    /// </summary>
    public bool CanBeModified => Status == OrderStatus.Pending;

    /// <summary>
    /// Checks if the order is in a final state
    /// </summary>
    public bool IsFinalState => Status is OrderStatus.Delivered or OrderStatus.Cancelled;

    /// <summary>
    /// Checks if the order can be cancelled
    /// </summary>
    public bool CanBeCancelled => Status == OrderStatus.Pending || Status == OrderStatus.Confirmed;

    /// <summary>
    /// Recalculates the total amount from all order items
    /// </summary>
    public void RecalculateTotal()
    {
        _totalAmount = OrderItems.Sum(item => item.Quantity * item.UnitPrice);
    }

    // Private helper methods
    private static string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"ORD-{timestamp}";
    }

    // Domain validation methods
    private static void ValidateCustomerId(Guid customerId)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));
        }
    }

    private static void ValidateCustomerName(string customerName)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ArgumentException("Customer name cannot be null or empty", nameof(customerName));
        }

        if (customerName.Length > 200)
        {
            throw new ArgumentException("Customer name cannot exceed 200 characters", nameof(customerName));
        }
    }

    private static void ValidateStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        var validTransitions = currentStatus switch
        {
            OrderStatus.Pending => [OrderStatus.Confirmed, OrderStatus.Cancelled],
            OrderStatus.Confirmed => [OrderStatus.Shipped, OrderStatus.Cancelled],
            OrderStatus.Shipped => [OrderStatus.Delivered],
            OrderStatus.Delivered => Array.Empty<OrderStatus>(), // Final state
            OrderStatus.Cancelled => Array.Empty<OrderStatus>(), // Final state
            _ => throw new ArgumentException($"Unknown order status: {currentStatus}")
        };

        if (!validTransitions.Contains(newStatus))
        {
            throw new InvalidOperationException(
                $"Invalid status transition from {currentStatus} to {newStatus}. " +
                $"Valid transitions are: {string.Join(", ", validTransitions)}");
        }
    }

    // Equality and comparison overrides
    public override bool Equals(object? obj)
    {
        if (obj is not Order other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Order? left, Order? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(Order? left, Order? right) => !(left == right);

    public override string ToString() =>
        $"Order: {OrderNumber} (Id: {Id}, Customer: {CustomerName}, Status: {Status}, Total: {TotalAmount:C}, Items: {_orderItems.Count})";
}