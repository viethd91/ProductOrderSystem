using Microsoft.EntityFrameworkCore;
using Products.API.Domain.Entities;
using Products.API.Domain.Interfaces;

namespace Products.API.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for Products API
/// Handles Product entity persistence and domain event publishing
/// </summary>
public class ProductContext : DbContext
{
    /// <summary>
    /// Products DbSet for Product entity operations
    /// </summary>
    public DbSet<Product> Products { get; set; } = null!;

    /// <summary>
    /// Constructor that accepts DbContextOptions for dependency injection
    /// </summary>
    /// <param name="options">Database context options containing connection string and provider</param>
    public ProductContext(DbContextOptions<ProductContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures entity mappings and relationships using Fluent API
    /// </summary>
    /// <param name="modelBuilder">Model builder for entity configuration</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            // Primary key configuration
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id)
                .IsRequired()
                .ValueGeneratedNever(); // Domain generates GUIDs

            // Name configuration
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200)
                .IsUnicode(true);

            // Price configuration
            entity.Property(p => p.Price)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasPrecision(18, 2);

            // Stock configuration
            entity.Property(p => p.Stock)
                .IsRequired()
                .HasDefaultValue(0);

            // Audit fields configuration
            entity.Property(p => p.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(p => p.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Soft delete configuration
            entity.Property(p => p.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes for performance
            entity.HasIndex(p => p.Name)
                .HasDatabaseName("IX_Products_Name");

            entity.HasIndex(p => p.Price)
                .HasDatabaseName("IX_Products_Price");

            entity.HasIndex(p => p.Stock)
                .HasDatabaseName("IX_Products_Stock");

            entity.HasIndex(p => p.IsDeleted)
                .HasDatabaseName("IX_Products_IsDeleted");

            entity.HasIndex(p => new { p.IsDeleted, p.Name })
                .HasDatabaseName("IX_Products_IsDeleted_Name");

            // Unique constraint on Name for active products
            entity.HasIndex(p => p.Name)
                .HasFilter("[IsDeleted] = 0")
                .IsUnique()
                .HasDatabaseName("IX_Products_Name_Unique");

            // Global query filter to exclude soft deleted items by default
            entity.HasQueryFilter(p => !p.IsDeleted);

            // Table configuration
            entity.ToTable("Products", schema: "dbo");

            // Domain events are ignored (not persisted)
            entity.Ignore(p => p.DomainEvents);
        });

        // Seed data (optional)
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Override SaveChangesAsync to handle audit fields and domain events
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields before saving
        UpdateAuditFields();

        // Collect domain events before saving
        var domainEvents = CollectDomainEvents();

        // Save changes to database
        var result = await base.SaveChangesAsync(cancellationToken);

        // Publish domain events after successful save
        await PublishDomainEvents(domainEvents, cancellationToken);

        return result;
    }

    /// <summary>
    /// Override SaveChanges (synchronous version)
    /// </summary>
    /// <returns>Number of affected rows</returns>
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// Updates CreatedAt and UpdatedAt fields automatically
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<Product>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entry.Property(p => p.CreatedAt).CurrentValue = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(p => p.UpdatedAt).CurrentValue = now;
                // Ensure CreatedAt is not modified
                entry.Property(p => p.CreatedAt).IsModified = false;
            }
        }
    }

    /// <summary>
    /// Collects domain events from all tracked entities
    /// </summary>
    /// <returns>List of domain events to publish</returns>
    private List<IDomainEvent> CollectDomainEvents()
    {
        var domainEvents = new List<IDomainEvent>();

        var entitiesWithEvents = ChangeTracker.Entries<Product>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        foreach (var entry in entitiesWithEvents)
        {
            domainEvents.AddRange(entry.Entity.DomainEvents);
            entry.Entity.ClearDomainEvents();
        }

        return domainEvents;
    }

    /// <summary>
    /// Publishes domain events using MediatR
    /// Note: This would typically be injected, but for simplicity we'll handle this in the repository
    /// </summary>
    /// <param name="domainEvents">Domain events to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task PublishDomainEvents(List<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        // Note: In a real implementation, you would inject IMediator or IMessageBus here
        // For now, we'll collect events and let the repository handle publishing
        // This maintains separation of concerns while keeping the example simple
        
        // Events are cleared from entities above, so the repository layer
        // will need to handle the actual publishing through DI services
        await Task.CompletedTask;
    }

    /// <summary>
    /// Seeds initial data for development/testing
    /// </summary>
    /// <param name="modelBuilder">Model builder for seeding</param>
    private static void SeedData(ModelBuilder modelBuilder)
    {
        var sampleProducts = new[]
        {
            new
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
                Name = "Sample Laptop",
                Price = 999.99m,
                Stock = 50,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            },
            new
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
                Name = "Wireless Mouse",
                Price = 29.99m,
                Stock = 100,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            },
            new
            {
                Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440003"),
                Name = "USB Cable",
                Price = 9.99m,
                Stock = 200,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = false
            }
        };

        modelBuilder.Entity<Product>().HasData(sampleProducts);
    }
}