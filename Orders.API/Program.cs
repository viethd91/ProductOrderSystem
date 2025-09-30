using System.Reflection;
using System.Linq;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Orders.API.Application.Behaviors;
using Orders.API.Application.IntegrationEvents;
using Orders.API.Application.IntegrationEvents.Handlers;
using Orders.API.Domain.Interfaces;
using Orders.API.Infrastructure.Data;
using Orders.API.Infrastructure.Messaging;
using Orders.API.Infrastructure.Repositories;
using Orders.API.Middleware;
using Serilog;
using Shared.IntegrationEvents;
using Shared.Messaging;
using IMessageBusAlias = Shared.Messaging.IMessageBus;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/orders-api-.txt", rollingInterval: RollingInterval.Day);
});

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Orders API",
        Version = "v1",
        Description = "Order management API with CQRS, DDD, and Clean Architecture patterns",
        Contact = new()
        {
            Name = "Orders API Team",
            Email = "orders-team@company.com"
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<OrderContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("Orders.API");
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// MediatR & pipeline
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Repos & messaging
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<Shared.Messaging.IMessageBus>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<FileBasedMessageBus>>();
    return new FileBasedMessageBus(logger);
});

// Integration event handlers (use fully qualified names to avoid ambiguity)
builder.Services.AddScoped<IIntegrationEventHandler<ProductPriceChangedIntegrationEvent>, Orders.API.Application.IntegrationEvents.Handlers.ProductPriceChangedEventHandler>();
builder.Services.AddScoped<IIntegrationEventHandler<ProductDeletedIntegrationEvent>, ProductDeletedEventHandlerStub>();
builder.Services.AddScoped<IIntegrationEventHandler<ProductCreatedIntegrationEvent>, ProductCreatedEventHandlerStub>();

builder.Services.AddHostedService<EventBusSubscriptionService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

    options.AddPolicy("OrdersPolicy", p =>
        p.WithOrigins("http://localhost:3000", "https://localhost:3000")
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials());
});

// Health checks, compression, caching
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderContext>("orders-database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded)
    .AddCheck("message-bus", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Message bus is running"));

builder.Services.AddResponseCompression();
builder.Services.AddResponseCaching();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Orders API v1");
    c.RoutePrefix = "swagger";
});

app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseResponseCaching();
app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// DB init
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<OrderContext>();

        if (app.Environment.IsDevelopment())
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            await context.Database.MigrateAsync();
        }

        app.Logger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

app.Logger.LogInformation("Orders.API starting up on https://localhost:5002...");
app.Run();

// Stub handlers - moved to bottom after app.Run()
public class ProductDeletedEventHandlerStub : IIntegrationEventHandler<ProductDeletedIntegrationEvent>
{
    private readonly ILogger<ProductDeletedEventHandlerStub> _logger;
    public ProductDeletedEventHandlerStub(ILogger<ProductDeletedEventHandlerStub> logger) => _logger = logger;
    public Task HandleAsync(ProductDeletedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received ProductDeletedIntegrationEvent for {ProductId}", @event.ProductId);
        return Task.CompletedTask;
    }
}

public class ProductCreatedEventHandlerStub : IIntegrationEventHandler<ProductCreatedIntegrationEvent>
{
    private readonly ILogger<ProductCreatedEventHandlerStub> _logger;
    public ProductCreatedEventHandlerStub(ILogger<ProductCreatedEventHandlerStub> logger) => _logger = logger;
    public Task HandleAsync(ProductCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received ProductCreatedIntegrationEvent for {ProductId}", @event.ProductId);
        return Task.CompletedTask;
    }
}

public partial class Program { }
