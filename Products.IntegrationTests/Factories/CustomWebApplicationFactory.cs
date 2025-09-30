using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Products.API.Infrastructure.Data;

namespace Products.IntegrationTests.Factories;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"Products_InMemory_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove existing ProductContext registrations (SQL Server)
            var dbContextDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ProductContext>));
            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            var contextDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(ProductContext));
            if (contextDescriptor is not null)
            {
                services.Remove(contextDescriptor);
            }

            // Add in-memory database
            services.AddDbContext<ProductContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });

            // Build provider and ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ProductContext>();
            ctx.Database.EnsureCreated();
        });
    }
}