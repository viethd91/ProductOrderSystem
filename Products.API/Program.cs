using System.Reflection;
using System.Linq;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Products.API.Application.Interfaces;
using Products.API.Domain.Interfaces;
using Products.API.Infrastructure.Data;
using Products.API.Infrastructure.Messaging;
using Products.API.Infrastructure.Repositories;
using Products.API.Middleware;
using Serilog;
using Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/products-api-.txt", rollingInterval: RollingInterval.Day);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Products API",
        Version = "v1",
        Description = "Product catalog management API with CQRS and Clean Architecture"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ProductContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("Products.API");
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Replace this line:
// builder.Services.AddSingleton<Shared.Messaging.IMessageBus>(_ => MessageBusConfiguration.GetSharedInstance());

// With one of these options:

// Option 1: RabbitMQ (requires RabbitMQ server)
//builder.Services.AddSingleton<Shared.Messaging.IMessageBus, RabbitMqMessageBus>();

// Option 2: HTTP-based
//builder.Services.AddHttpClient();
//builder.Services.AddSingleton<Shared.Messaging.IMessageBus>(sp =>
//{
//    var httpClient = sp.GetRequiredService<HttpClient>();
//    var logger = sp.GetRequiredService<ILogger<HttpMessageBus>>();
//    return new HttpMessageBus(httpClient, logger);
//});

// Option 3: File-based (simplest for demo)
builder.Services.AddSingleton<Shared.Messaging.IMessageBus, FileBasedMessageBus>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddScoped<IEventPublisher, EventPublisher>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("ProductsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductContext>(
        name: "products-database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded)
    .AddCheck("message-bus", () =>
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Message bus is running"));

builder.Services.AddResponseCompression();
builder.Services.AddResponseCaching();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Products API v1");
        c.RoutePrefix = string.Empty;
    });
}

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

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ProductContext>();

        if (!context.Database.IsRelational())
        {
            await context.Database.EnsureCreatedAsync();
        }
        else if (app.Environment.IsDevelopment())
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

app.Logger.LogInformation("Products.API starting up...");
app.Run();

public partial class Program { }
