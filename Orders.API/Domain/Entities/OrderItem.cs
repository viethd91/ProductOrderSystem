namespace Orders.API.Domain.Entities;

/// <summary>
/// Order item entity representing a product within an order
/// Child entity of the Order aggregate root
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Unique identifier for the order item
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Identifier of the order this item belongs to
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// Product identifier from the Products catalog
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Product name at the time of order (for historical purposes)
    /// </summary>
    public string ProductName { get; private set; }

    /// <summary>
    /// Quantity of the product ordered
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Unit price of the product at the time of order
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Total price for this line item (Quantity × UnitPrice)
    /// Calculated property that ensures consistency
    /// </summary>
    public decimal TotalPrice => CalculateSubtotal();

    /// <summary>
    /// Navigation property to the parent Order aggregate
    /// </summary>
    public Order Order { get; private set; } = null!;

    /// <summary>
    /// Primary constructor for creating a new order item
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="productName">Product name</param>
    /// <param name="quantity">Quantity ordered</param>
    /// <param name="unitPrice">Unit price</param>
    public OrderItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        ValidateProductId(productId);
        ValidateProductName(productName);
        ValidateQuantity(quantity);
        ValidateUnitPrice(unitPrice);

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    /// <summary>
    /// Parameterless constructor for EF Core
    /// </summary>
    private OrderItem()
    {
        ProductName = string.Empty;
    }

    /// <summary>
    /// Updates the quantity of this order item
    /// Used when consolidating items with the same product
    /// </summary>
    /// <param name="newQuantity">New quantity value</param>
    /// <exception cref="ArgumentException">Thrown when quantity is invalid</exception>
    public void UpdateQuantity(int newQuantity)
    {
        ValidateQuantity(newQuantity);
        Quantity = newQuantity;
    }

    /// <summary>
    /// Updates the unit price of this order item
    /// Used when product price changes before order confirmation
    /// </summary>
    /// <param name="newUnitPrice">New unit price value</param>
    /// <exception cref="ArgumentException">Thrown when price is invalid</exception>
    public void UpdatePrice(decimal newUnitPrice)
    {
        ValidateUnitPrice(newUnitPrice);
        UnitPrice = newUnitPrice;
    }

    /// <summary>
    /// Updates the product name (in case of product name changes)
    /// </summary>
    /// <param name="newProductName">New product name</param>
    /// <exception cref="ArgumentException">Thrown when name is invalid</exception>
    public void UpdateProductName(string newProductName)
    {
        ValidateProductName(newProductName);
        ProductName = newProductName;
    }

    /// <summary>
    /// Updates the unit price of this order item to a new value
    /// </summary>
    /// <param name="newPrice">New unit price value</param>
    /// <exception cref="ArgumentException">Thrown when price is negative</exception>
    public void UpdateUnitPrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Unit price cannot be negative", nameof(newPrice));
        
        UnitPrice = newPrice;
    }

    /// <summary>
    /// Calculates the subtotal for this order item
    /// </summary>
    /// <returns>The subtotal (Quantity × UnitPrice)</returns>
    public decimal CalculateSubtotal()
    {
        return Quantity * UnitPrice;
    }

    /// <summary>
    /// Increases the quantity by the specified amount
    /// </summary>
    /// <param name="additionalQuantity">Amount to add to current quantity</param>
    /// <exception cref="ArgumentException">Thrown when additional quantity is invalid</exception>
    public void AddQuantity(int additionalQuantity)
    {
        if (additionalQuantity <= 0)
        {
            throw new ArgumentException("Additional quantity must be positive", nameof(additionalQuantity));
        }

        var newQuantity = Quantity + additionalQuantity;
        ValidateQuantity(newQuantity);
        Quantity = newQuantity;
    }

    /// <summary>
    /// Reduces the quantity by the specified amount
    /// </summary>
    /// <param name="quantityToRemove">Amount to remove from current quantity</param>
    /// <exception cref="ArgumentException">Thrown when quantity to remove is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when resulting quantity would be invalid</exception>
    public void RemoveQuantity(int quantityToRemove)
    {
        if (quantityToRemove <= 0)
        {
            throw new ArgumentException("Quantity to remove must be positive", nameof(quantityToRemove));
        }

        var newQuantity = Quantity - quantityToRemove;
        if (newQuantity <= 0)
        {
            throw new InvalidOperationException("Cannot reduce quantity to zero or negative. Remove the item instead.");
        }

        Quantity = newQuantity;
    }

    /// <summary>
    /// Sets the order identifier (used by EF Core)
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    internal void SetOrderId(Guid orderId)
    {
        OrderId = orderId;
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

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        }

        // Add reasonable upper limit to prevent abuse
        if (quantity > 10000)
        {
            throw new ArgumentException("Quantity cannot exceed 10,000 items", nameof(quantity));
        }
    }

    private static void ValidateUnitPrice(decimal unitPrice)
    {
        if (unitPrice <= 0)
        {
            throw new ArgumentException("Unit price must be greater than zero", nameof(unitPrice));
        }

        // Check for reasonable decimal precision (max 4 decimal places for extended precision)
        if (decimal.Round(unitPrice, 4) != unitPrice)
        {
            throw new ArgumentException("Unit price can have at most 4 decimal places", nameof(unitPrice));
        }

        // Add reasonable upper limit
        if (unitPrice > 1_000_000m)
        {
            throw new ArgumentException("Unit price cannot exceed 1,000,000", nameof(unitPrice));
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
        $"OrderItem: {ProductName} (Id: {Id}, ProductId: {ProductId}, Qty: {Quantity}, Price: {UnitPrice:C}, Total: {TotalPrice:C})";
}