using System.ComponentModel.DataAnnotations;

namespace Products.API.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Primary constructor with domain validation
    public Product(string name, decimal price, int stock)
    {
        ValidateName(name);
        ValidatePrice(price);
        ValidateStock(stock);

        Id = Guid.NewGuid();
        Name = name;
        Price = price;
        Stock = stock;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    // Parameterless constructor for EF Core
    private Product() 
    {
        Name = string.Empty;
    }

    public void UpdatePrice(decimal newPrice)
    {
        ValidatePrice(newPrice);
        
        if (Price != newPrice)
        {
            Price = newPrice;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateStock(int newStock)
    {
        ValidateStock(newStock);
        
        if (Stock != newStock)
        {
            Stock = newStock;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateName(string newName)
    {
        ValidateName(newName);
        
        if (Name != newName)
        {
            Name = newName;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Delete(string? reason = null, string? deletedBy = null)
    {
        if (IsDeleted)
            throw new InvalidOperationException("Product is already deleted");

        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        if (Stock < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {Stock}, Requested: {quantity}");

        Stock -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        Stock += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsInStock => Stock > 0 && !IsDeleted;

    public bool IsAvailable(int requestedQuantity) => Stock >= requestedQuantity && !IsDeleted;

    // Domain validation methods
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Product name cannot exceed 200 characters", nameof(name));
    }

    private static void ValidatePrice(decimal price)
    {
        if (price < 0)
            throw new ArgumentException("Product price must be non-negative", nameof(price));

        if (price == 0)
            throw new ArgumentException("Product price must be greater than zero", nameof(price));

        // Check for reasonable decimal precision (max 2 decimal places for currency)
        if (decimal.Round(price, 2) != price)
            throw new ArgumentException("Product price can have at most 2 decimal places", nameof(price));
    }

    private static void ValidateStock(int stock)
    {
        if (stock < 0)
            throw new ArgumentException("Product stock cannot be negative", nameof(stock));
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Product other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Product? left, Product? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(Product? left, Product? right) => !(left == right);

    public override string ToString() => $"Product: {Name} (Id: {Id}, Price: {Price:C}, Stock: {Stock}, Deleted: {IsDeleted})";
}