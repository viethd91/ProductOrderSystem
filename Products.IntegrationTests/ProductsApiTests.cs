using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Products.API.Domain.Entities;
using Products.API.Domain.Interfaces;
using Products.IntegrationTests.Factories;

namespace Products.IntegrationTests;

public class ProductsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct_WithValidData()
    {
        // Arrange
        var payload = new
        {
            name = "Integration Product A",
            price = 15.25m,
            stock = 40
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string? name = TryGet(root, "name") ?? TryGet(root, "Name");
        decimal? price = TryGetDecimal(root, "price") ?? TryGetDecimal(root, "Price");
        int? stock = TryGetInt(root, "stock") ?? TryGetInt(root, "Stock");

        name.Should().Be(payload.name);
        price.Should().Be(payload.price);
        stock.Should().Be(payload.stock);

        Guid.TryParse((TryGet(root, "id") ?? TryGet(root, "Id")), out var id).Should().BeTrue();
        id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        // Arrange (seed 2 products)
        await SeedProductsAsync([
            new("Integration Seed 1", 5.99m, 5),
            new("Integration Seed 2", 9.49m, 10)
        ]);

        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.ValueKind.Should().Be(JsonValueKind.Array);

        var names = root.EnumerateArray()
            .Select(e => TryGet(e, "name") ?? TryGet(e, "Name"))
            .Where(n => n is not null)
            .ToList();

        names.Should().Contain("Integration Seed 1");
        names.Should().Contain("Integration Seed 2");
    }

    private async Task SeedProductsAsync(IEnumerable<Product> products)
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        foreach (var p in products)
        {
            await repo.AddAsync(p);
        }

        await repo.SaveChangesAsync();
    }

    private static string? TryGet(JsonElement element, string property)
        => element.TryGetProperty(property, out var v) ? v.GetString() : null;

    private static decimal? TryGetDecimal(JsonElement element, string property)
        => element.TryGetProperty(property, out var v) && v.TryGetDecimal(out var d) ? d : null;

    private static int? TryGetInt(JsonElement element, string property)
        => element.TryGetProperty(property, out var v) && v.TryGetInt32(out var i) ? i : null;
}