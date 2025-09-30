# .NET 8 Comprehensive Demo API Project

## Project Overview
Build a **Product Order Management System** with microservices architecture demonstrating modern .NET 8 practices.

## Architecture
API Gateway pattern with two microservices:
1. **API Gateway** - Single entry point (using YARP - Yet Another Reverse Proxy)
2. **Products.API** - Product catalog management
3. **Orders.API** - Order processing

Microservices communicate via message bus for async operations.

## Technology Stack

### Core Framework
- .NET 8
- C# 12 features (primary constructors, collection expressions, etc.)
- ASP.NET Core Web API

### Patterns & Practices
- **CQRS** with MediatR
- **Domain-Driven Design** (Entities, Value Objects, Aggregates)
- **Repository Pattern**
- **Unit of Work Pattern**
- **Dependency Injection** (Scoped, Transient, Singleton)

### Database & ORM
- **Entity Framework Core 8**
- **SQL Server** (LocalDB for demo)
- Code-First migrations
- **In-Memory Database** for testing

### Messaging
- **RabbitMQ** or **MassTransit** with In-Memory transport for demo
- Event-driven communication between services
- Domain Events pattern

### Validation & Mapping
- **FluentValidation**
- **AutoMapper**

### Testing
- **xUnit**
- **Moq** - Mocking framework
- **FluentAssertions**
- **Integration tests** with WebApplicationFactory
- **Unit tests** for handlers, services, and domain logic

### API Documentation
- **Swagger/OpenAPI** with Swashbuckle
- XML documentation comments

### Cross-Cutting Concerns
- **Serilog** - Structured logging
- **Polly** - Resilience and transient-fault-handling
- Global exception handling middleware
- **Health Checks**
- **Response compression**
- **CORS configuration**

### API Gateway
- **YARP (Yet Another Reverse Proxy)** - Microsoft's modern reverse proxy
- Route aggregation
- Request/Response transformation
- Rate limiting at gateway level
- Authentication/Authorization centralization

## Project Structure

```
ProductOrderSystem/
├── src/
│   ├── Gateway.API/
│   │   ├── appsettings.json
│   │   ├── Middleware/
│   │   └── Program.cs
│   │
│   ├── Products.API/
│   │   ├── Controllers/
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   ├── Handlers/
│   │   │   ├── Validators/
│   │   │   └── DTOs/
│   │   ├── Domain/
│   │   │   ├── Entities/
│   │   │   ├── Events/
│   │   │   └── Interfaces/
│   │   ├── Infrastructure/
│   │   │   ├── Data/
│   │   │   ├── Repositories/
│   │   │   └── Messaging/
│   │   ├── Middleware/
│   │   └── Program.cs
│   │
│   ├── Orders.API/
│   │   ├── Controllers/
│   │   ├── Application/
│   │   ├── Domain/
│   │   ├── Infrastructure/
│   │   ├── Middleware/
│   │   └── Program.cs
│   │
│   └── Shared/
│       ├── Events/
│       └── Common/
│
├── tests/
│   ├── Products.UnitTests/
│   ├── Products.IntegrationTests/
│   ├── Orders.UnitTests/
│   └── Orders.IntegrationTests/
│
└── ProductOrderSystem.sln
```

## Implementation Details

### 1. API Gateway (YARP)

#### Configuration (appsettings.json)
```json
{
  "ReverseProxy": {
    "Routes": {
      "products-route": {
        "ClusterId": "products-cluster",
        "Match": {
          "Path": "/products/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/products/{**catch-all}" }
        ]
      },
      "orders-route": {
        "ClusterId": "orders-cluster",
        "Match": {
          "Path": "/orders/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/orders/{**catch-all}" }
        ]
      }
    },
    "Clusters": {
      "products-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "https://localhost:5001"
          }
        }
      },
      "orders-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "https://localhost:5002"
          }
        }
      }
    }
  }
}
```

#### Program.cs
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
    });
});

// Add authentication (optional)
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://your-identity-server";
        options.TokenValidationParameters = new()
        {
            ValidateAudience = false
        };
    });

builder.Services.AddCors();

var app = builder.Build();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();
```

#### Benefits Demonstrated
- **Single entry point** - Clients only know about gateway
- **Service discovery** - Gateway knows backend service locations
- **Cross-cutting concerns** - Auth, rate limiting, CORS at one place
- **Routing logic** - Path transformation and aggregation
- **Load balancing** - YARP supports multiple destinations
- **Circuit breaker** - Can add with Polly integration

### 2. Products.API

#### Endpoints
- `POST /api/products` - Create product (CQRS Command)
- `GET /api/products` - Get all products (CQRS Query)
- `GET /api/products/{id}` - Get product by ID (CQRS Query)
- `PUT /api/products/{id}` - Update product (CQRS Command)
- `DELETE /api/products/{id}` - Delete product (CQRS Command)

#### Domain Events
- `ProductCreatedEvent`
- `ProductPriceChangedEvent`
- `ProductDeletedEvent`

#### Key Features
- Product entity with validation
- Price as Value Object
- Repository with EF Core
- Publish domain events to message bus
- FluentValidation for commands
- AutoMapper for DTOs

### 2. Orders.API

#### Endpoints
- `POST /api/orders` - Create order (CQRS Command)
- `GET /api/orders` - Get all orders (CQRS Query)
- `GET /api/orders/{id}` - Get order by ID (CQRS Query)
- `PUT /api/orders/{id}/status` - Update order status (CQRS Command)

#### Domain Events
- `OrderCreatedEvent`
- `OrderStatusChangedEvent`

#### Key Features
- Order aggregate with OrderItems
- Listen to `ProductPriceChangedEvent` from Products.API
- Order state machine (Pending → Confirmed → Shipped → Delivered)
- Calculate total price from order items
- Integration event handlers

### 3. Dependency Injection Examples

**Program.cs should demonstrate:**

```csharp
// Scoped - per request lifetime
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Transient - new instance every time
builder.Services.AddTransient<IEmailService, EmailService>();

// Singleton - single instance for application lifetime
builder.Services.AddSingleton<IMessageBus, InMemoryMessageBus>();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// MediatR for CQRS
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// FluentValidation
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
```

### 4. CQRS Implementation with MediatR

**Command Example:**
```csharp
public record CreateProductCommand(string Name, decimal Price, int Stock) 
    : IRequest<ProductDto>;

public class CreateProductCommandHandler(
    IProductRepository repository,
    IMapper mapper,
    IMessageBus messageBus) 
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(
        CreateProductCommand request, 
        CancellationToken cancellationToken)
    {
        var product = new Product(request.Name, request.Price, request.Stock);
        await repository.AddAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        
        // Publish domain event
        await messageBus.PublishAsync(
            new ProductCreatedEvent(product.Id, product.Name, product.Price));
        
        return mapper.Map<ProductDto>(product);
    }
}
```

**Query Example:**
```csharp
public record GetProductsQuery : IRequest<List<ProductDto>>;

public class GetProductsQueryHandler(
    IProductRepository repository,
    IMapper mapper) 
    : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(
        GetProductsQuery request, 
        CancellationToken cancellationToken)
    {
        var products = await repository.GetAllAsync(cancellationToken);
        return mapper.Map<List<ProductDto>>(products);
    }
}
```

### 5. Message Bus Implementation

**Interface:**
```csharp
public interface IMessageBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) 
        where T : class;
    
    void Subscribe<T>(Func<T, Task> handler) where T : class;
}
```

**In-Memory Implementation (Singleton):**
```csharp
public class InMemoryMessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) 
        where T : class
    {
        var messageType = typeof(T);
        if (_handlers.TryGetValue(messageType, out var handlers))
        {
            var tasks = handlers
                .Cast<Func<T, Task>>()
                .Select(handler => handler(message));
            return Task.WhenAll(tasks);
        }
        return Task.CompletedTask;
    }
    
    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        var messageType = typeof(T);
        _handlers.AddOrUpdate(
            messageType,
            _ => [handler],
            (_, existing) => { existing.Add(handler); return existing; });
    }
}
```

### 6. Testing Strategy

#### Unit Tests
```csharp
public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesProduct()
    {
        // Arrange
        var mockRepo = new Mock<IProductRepository>();
        var mockMapper = new Mock<IMapper>();
        var mockBus = new Mock<IMessageBus>();
        
        var handler = new CreateProductCommandHandler(
            mockRepo.Object, 
            mockMapper.Object, 
            mockBus.Object);
        
        var command = new CreateProductCommand("Test", 10.99m, 100);
        
        // Act
        await handler.Handle(command, CancellationToken.None);
        
        // Assert
        mockRepo.Verify(r => r.AddAsync(
            It.IsAny<Product>(), 
            It.IsAny<CancellationToken>()), Times.Once);
        
        mockBus.Verify(b => b.PublishAsync(
            It.IsAny<ProductCreatedEvent>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

#### Integration Tests
```csharp
public class ProductsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public ProductsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace SQL Server with In-Memory database
                services.RemoveAll<DbContextOptions<ProductContext>>();
                services.AddDbContext<ProductContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();
    }
    
    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct()
    {
        // Arrange
        var command = new { Name = "Test Product", Price = 10.99, Stock = 100 };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/products", command);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Name.Should().Be("Test Product");
    }
}
```

### 7. Middleware Examples

**Global Exception Handler:**
```csharp
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

### 8. C# 12 Features to Showcase

- **Primary constructors** in classes
- **Collection expressions**: `[item1, item2]`
- **Required members** with `required` keyword
- **Init-only properties**
- **Record types** for DTOs and commands
- **Pattern matching** enhancements
- **Global using directives**
- **File-scoped namespaces**

### 9. Best Practices Demonstrated

✅ Clean Architecture / Onion Architecture
✅ SOLID principles
✅ Separation of Concerns
✅ Async/await throughout
✅ CancellationToken propagation
✅ Structured logging with Serilog
✅ Configuration via appsettings.json and environment variables
✅ Health checks for database and message bus
✅ API versioning
✅ Response DTOs (never expose entities)
✅ Validation at application boundary
✅ Domain events for loose coupling
✅ Repository abstraction over EF Core
✅ Testable architecture

## Running the Demo

### Prerequisites
```bash
# .NET 8 SDK
# SQL Server LocalDB (or Docker)
# Optional: RabbitMQ (or use in-memory bus)
```

### Steps
```bash
# Restore packages
dotnet restore

# Run migrations
dotnet ef database update --project src/Products.API
dotnet ef database update --project src/Orders.API

# Run all services (in separate terminals)
dotnet run --project src/Gateway.API
dotnet run --project src/Products.API
dotnet run --project src/Orders.API

# Run tests
dotnet test
```

### Access URLs
- **API Gateway**: `https://localhost:5000`
  - Products: `https://localhost:5000/products`
  - Orders: `https://localhost:5000/orders`
- Products API (direct): `https://localhost:5001/swagger`
- Orders API (direct): `https://localhost:5002/swagger`
- Gateway Swagger: `https://localhost:5000/swagger` (aggregated)

## Interview Talking Points

1. **Why API Gateway?** 
   - Single entry point for clients
   - Centralized authentication/authorization
   - Rate limiting and throttling at gateway level
   - Service discovery and routing
   - Request/response aggregation
   - Backend services can be scaled/moved without client changes

2. **Why YARP over Ocelot?**
   - Modern, actively maintained by Microsoft
   - Built on .NET's reverse proxy infrastructure
   - Better performance
   - Native .NET 8 integration
   - Supports all HTTP protocols including HTTP/2, HTTP/3

3. **Why CQRS?** - Separation of read and write concerns, optimized queries, scalability

4. **Microservices communication** - Async messaging vs sync HTTP, eventual consistency

5. **DI Lifetimes** - When to use Scoped vs Singleton vs Transient

6. **Testing strategy** - Unit tests for business logic, integration tests for APIs

7. **Domain Events** - Decoupling, side effects, eventual consistency

8. **EF Core best practices** - Change tracking, AsNoTracking for queries, migrations

9. **Performance** - Response caching, compression, connection pooling

10. **Observability** - Structured logging, health checks, correlation IDs across services

## Extensions for Discussion

- **BFF (Backend for Frontend)** pattern - Separate gateways for web/mobile
- Add Redis for distributed caching at gateway level
- Implement Outbox pattern for reliable messaging
- Service mesh (Linkerd/Istio) vs API Gateway - when to use what
- Docker containerization with docker-compose
- Kubernetes deployment with Ingress controller
- Event Sourcing
- Saga pattern for distributed transactions
- gRPC for internal service communication
- **API Gateway aggregation** - Combine multiple backend calls into one response
- **OpenTelemetry** - Distributed tracing across gateway and services

---

This project demonstrates production-ready patterns in a simple, understandable domain that you can explain confidently in 30-60 minutes.