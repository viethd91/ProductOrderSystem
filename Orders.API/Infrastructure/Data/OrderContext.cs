using Microsoft.EntityFrameworkCore;
using Orders.API.Domain.Entities;
using Orders.API.Domain.Enums;

namespace Orders.API.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for Orders API
/// Handles data access for Order aggregate and related entities
/// </summary>
public class OrderContext : DbContext
{
    /// <summary>
    /// Orders DbSet for Order aggregate root
    /// </summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>
    /// OrderItems DbSet for OrderItem child entities
    /// </summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <summary>
    /// Constructor for dependency injection
    /// </summary>
    /// <param name="options">DbContext options with connection string and provider</param>
    public OrderContext(DbContextOptions<OrderContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configure entity relationships and database schema
    /// </summary>
    /// <param name="modelBuilder">Entity Framework model builder</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Order entity
        ConfigureOrderEntity(modelBuilder);
        
        // Configure OrderItem entity
        ConfigureOrderItemEntity(modelBuilder);

        // Add seed data for development
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Override SaveChangesAsync to handle audit fields automatically
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected records</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Override SaveChanges to handle audit fields automatically
    /// </summary>
    /// <returns>Number of affected records</returns>
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// Configure Order entity mapping
    /// </summary>
    /// <param name="modelBuilder">Entity Framework model builder</param>
    private static void ConfigureOrderEntity(ModelBuilder modelBuilder)
    {
        var orderEntity = modelBuilder.Entity<Order>();

        // Table configuration
        orderEntity.ToTable("Orders", "dbo");

        // Primary key
        orderEntity.HasKey(o => o.Id);
        orderEntity.Property(o => o.Id)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        // OrderNumber configuration
        orderEntity.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50)
            .IsUnicode(false);

        // Customer information
        orderEntity.Property(o => o.CustomerId)
            .IsRequired()
            .HasColumnType("uniqueidentifier");

        orderEntity.Property(o => o.CustomerName)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true);

        // Financial fields
        orderEntity.Property(o => o.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        // Status as integer (enum)
        orderEntity.Property(o => o.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(OrderStatus.Pending);

        // Date fields
        orderEntity.Property(o => o.OrderDate)
            .IsRequired()
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        orderEntity.Property(o => o.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        orderEntity.Property(o => o.UpdatedAt)
            .IsRequired()
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for performance
        orderEntity.HasIndex(o => o.OrderNumber)
            .IsUnique()
            .HasDatabaseName("IX_Orders_OrderNumber_Unique");

        orderEntity.HasIndex(o => o.CustomerId)
            .HasDatabaseName("IX_Orders_CustomerId");

        orderEntity.HasIndex(o => o.Status)
            .HasDatabaseName("IX_Orders_Status");

        orderEntity.HasIndex(o => o.OrderDate)
            .HasDatabaseName("IX_Orders_OrderDate");

        orderEntity.HasIndex(o => o.TotalAmount)
            .HasDatabaseName("IX_Orders_TotalAmount");

        // Composite indexes for common queries
        orderEntity.HasIndex(o => new { o.CustomerId, o.Status })
            .HasDatabaseName("IX_Orders_CustomerId_Status");

        orderEntity.HasIndex(o => new { o.Status, o.OrderDate })
            .HasDatabaseName("IX_Orders_Status_OrderDate");

        // Relationship with OrderItems (one-to-many with cascade delete)
        orderEntity.HasMany<OrderItem>()
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filter to exclude cancelled orders by default
        // This can be overridden with IgnoreQueryFilters() when needed
        orderEntity.HasQueryFilter(o => o.Status != OrderStatus.Cancelled);

        // Ignore domain events collection for EF Core
        orderEntity.Ignore(o => o.DomainEvents);
    }

    /// <summary>
    /// Configure OrderItem entity mapping
    /// </summary>
    /// <param name="modelBuilder">Entity Framework model builder</param>
    private static void ConfigureOrderItemEntity(ModelBuilder modelBuilder)
    {
        var orderItemEntity = modelBuilder.Entity<OrderItem>();

        // Table configuration
        orderItemEntity.ToTable("OrderItems", "dbo");

        // Primary key
        orderItemEntity.HasKey(oi => oi.Id);
        orderItemEntity.Property(oi => oi.Id)
            .HasColumnType("uniqueidentifier")
            .IsRequired();

        // Foreign key to Order
        orderItemEntity.Property(oi => oi.OrderId)
            .IsRequired()
            .HasColumnType("uniqueidentifier");

        // Product information
        orderItemEntity.Property(oi => oi.ProductId)
            .IsRequired()
            .HasColumnType("uniqueidentifier");

        orderItemEntity.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(200)
            .IsUnicode(true);

        // Quantity
        orderItemEntity.Property(oi => oi.Quantity)
            .IsRequired()
            .HasDefaultValue(1);

        // Financial fields
        orderItemEntity.Property(oi => oi.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        // Computed column for TotalPrice (Quantity * UnitPrice)
        // Note: EF Core will ignore this since TotalPrice is a computed property in the domain model
        orderItemEntity.Ignore(oi => oi.TotalPrice);

        // Indexes for performance
        orderItemEntity.HasIndex(oi => oi.OrderId)
            .HasDatabaseName("IX_OrderItems_OrderId");

        orderItemEntity.HasIndex(oi => oi.ProductId)
            .HasDatabaseName("IX_OrderItems_ProductId");

        // Composite index for unique product per order (business rule enforcement)
        orderItemEntity.HasIndex(oi => new { oi.OrderId, oi.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_OrderItems_OrderId_ProductId_Unique");

        // Configure the relationship (many-to-one with Order)
        orderItemEntity.HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }

    /// <summary>
    /// Seed initial data for development and testing
    /// </summary>
    /// <param name="modelBuilder">Entity Framework model builder</param>
    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Orders
        var sampleOrderId1 = new Guid("660e8400-e29b-41d4-a716-446655440001");
        var sampleOrderId2 = new Guid("660e8400-e29b-41d4-a716-446655440002");
        
        var baseDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Order>().HasData(
            new
            {
                Id = sampleOrderId1,
                OrderNumber = "ORD-20240115103000",
                CustomerId = new Guid("770e8400-e29b-41d4-a716-446655440001"),
                CustomerName = "John Doe",
                OrderDate = baseDate,
                Status = OrderStatus.Pending,
                TotalAmount = 1029.98m,
                CreatedAt = baseDate,
                UpdatedAt = baseDate
            },
            new
            {
                Id = sampleOrderId2,
                OrderNumber = "ORD-20240115110000",
                CustomerId = new Guid("770e8400-e29b-41d4-a716-446655440002"),
                CustomerName = "Jane Smith",
                OrderDate = baseDate.AddMinutes(30),
                Status = OrderStatus.Confirmed,
                TotalAmount = 39.98m,
                CreatedAt = baseDate.AddMinutes(30),
                UpdatedAt = baseDate.AddMinutes(30)
            }
        );

        // Seed OrderItems (matching Products.API seed data)
        modelBuilder.Entity<OrderItem>().HasData(
            // Order 1 items
            new
            {
                Id = new Guid("880e8400-e29b-41d4-a716-446655440001"),
                OrderId = sampleOrderId1,
                ProductId = new Guid("550e8400-e29b-41d4-a716-446655440001"), // Sample Laptop
                ProductName = "Sample Laptop",
                Quantity = 1,
                UnitPrice = 999.99m
            },
            new
            {
                Id = new Guid("880e8400-e29b-41d4-a716-446655440002"),
                OrderId = sampleOrderId1,
                ProductId = new Guid("550e8400-e29b-41d4-a716-446655440002"), // Wireless Mouse
                ProductName = "Wireless Mouse",
                Quantity = 1,
                UnitPrice = 29.99m
            },
            // Order 2 items
            new
            {
                Id = new Guid("880e8400-e29b-41d4-a716-446655440003"),
                OrderId = sampleOrderId2,
                ProductId = new Guid("550e8400-e29b-41d4-a716-446655440002"), // Wireless Mouse
                ProductName = "Wireless Mouse",
                Quantity = 1,
                UnitPrice = 29.99m
            },
            new
            {
                Id = new Guid("880e8400-e29b-41d4-a716-446655440004"),
                OrderId = sampleOrderId2,
                ProductId = new Guid("550e8400-e29b-41d4-a716-446655440003"), // USB Cable
                ProductName = "USB Cable",
                Quantity = 1,
                UnitPrice = 9.99m
            }
        );
    }

    /// <summary>
    /// Update audit fields for entities being saved
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Order && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var order = (Order)entityEntry.Entity;
            
            if (entityEntry.State == EntityState.Added)
            {
                // CreatedAt is set in the domain entity constructor
                // UpdatedAt is also set in the domain entity constructor
            }
            else if (entityEntry.State == EntityState.Modified)
            {
                // UpdatedAt should be set by domain methods, but ensure it's current
                entityEntry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}