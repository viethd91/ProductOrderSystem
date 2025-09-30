using System.ComponentModel.DataAnnotations;

namespace Orders.API.Domain.Entities;

/// <summary>
/// Order item entity representing a line item in an order
/// Contains product information and pricing at the time of order
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Unique identifier for the order item
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Identifier of the product this item represents
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Product name at the time of order (for historical reference)
    /// </summary>
    public string ProductName { get; private set; }

    /// <summary>
    /// Unit price of the product at the time of order
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Quantity of the product ordered
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Total price for this line item (UnitPrice * Quantity)
    /// </summary>
    public decimal TotalPrice => UnitPrice * Quantity;

    /// <summary>
    /// Reference to the parent order
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// Navigation property to the parent order
    /// </summary>
    public Order Order { get; private set; } = null!;

    /// <summary>
    /// Primary constructor for creating a new order item
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="productName">Product name</param>
    /// <param name="unitPrice">Unit price</param>
    /// <param name="quantity">Quantity ordered</param>
    public OrderItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        ValidateProductId(productId);
        ValidateProductName(productName);
        ValidateUnitPrice(unitPrice);
        ValidateQuantity(quantity);

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    /// <summary>
    /// Parameterless constructor for EF Core
    /// </summary>
    private OrderItem()
    {
        ProductName = string.Empty;
    }

    /// <summary>
    /// Updates the unit price of the order item
    /// Typically used for pending orders when product prices change
    /// </summary>
    /// <param name="newUnitPrice">New unit price</param>
    /// <exception cref="ArgumentException">Thrown when price is invalid</exception>
    public void UpdateUnitPrice(decimal newUnitPrice)
    {
        ValidateUnitPrice(newUnitPrice);
        UnitPrice = newUnitPrice;
    }

    /// <summary>
    /// Updates the quantity of the order item
    /// </summary>
    /// <param name="newQuantity">New quantity</param>
    /// <exception cref="ArgumentException">Thrown when quantity is invalid</exception>
    public void UpdateQuantity(int newQuantity)
    {
        ValidateQuantity(newQuantity);
        Quantity = newQuantity;
    }

    /// <summary>
    /// Updates the product name (for historical accuracy)
    /// </summary>
    /// <param name="newProductName">New product name</param>
    /// <exception cref="ArgumentException">Thrown when name is invalid</exception>
    public void UpdateProductName(string newProductName)
    {
        ValidateProductName(newProductName);
        ProductName = newProductName;
    }

    // Domain validation methods
    private static void ValidateProductId(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product ID cannot be empty", nameof(productId));
        }
    }

    private static void ValidateProductName(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name cannot be null or empty", nameof(productName));
        }

        if (productName.Length > 200)
        {
            throw new ArgumentException("Product name cannot exceed 200 characters", nameof(productName));
        }
    }

    private static void ValidateUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));
        }

        if (unitPrice == 0)
        {
            throw new ArgumentException("Unit price must be greater than zero", nameof(unitPrice));
        }

        // Check for reasonable decimal precision (max 2 decimal places for currency)
        if (decimal.Round(unitPrice, 2) != unitPrice)
        {
            throw new ArgumentException("Unit price can have at most 2 decimal places", nameof(unitPrice));
        }
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        }

        if (quantity > 10000)
        {
            throw new ArgumentException("Quantity cannot exceed 10,000 units per line item", nameof(quantity));
        }
    }

    // Equality and comparison overrides
    public override bool Equals(object? obj)
    {
        if (obj is not OrderItem other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(OrderItem? left, OrderItem? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(OrderItem? left, OrderItem? right) => !(left == right);

    public override string ToString() =>
        $"OrderItem: {ProductName} (Id: {Id}, ProductId: {ProductId}, Quantity: {Quantity}, UnitPrice: {UnitPrice:C}, Total: {TotalPrice:C})";
}