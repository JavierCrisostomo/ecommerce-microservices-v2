using System.Net;
using System.Net.Http.Json;
using Catalog.Api.Contracts;
using Catalog.Application.Products;
using Catalog.Application.Products.Commands.CreateProduct;
using FluentAssertions;
using IntegrationTests.Shared;
using Xunit;

namespace Catalog.IntegrationTests;

public class ProductEndpointsTests : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private readonly CatalogApiFactory _factory;
    private HttpClient _client = null!;

    public ProductEndpointsTests(SqlServerFixture sqlFixture)
    {
        _factory = new CatalogApiFactory(sqlFixture.GetConnectionString("catalog_db_it"));
    }

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateProduct_ThenGetById_ReturnsItFromReadModel()
    {
        var create = await _client.PostAsJsonAsync("/api/products/", new CreateProductRequest("book-001", "Clean Architecture", "Un libro", "Books", 45.90m));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<CreateProductResult>();

        var getResponse = await _client.GetAsync($"/api/products/{created!.ProductId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await getResponse.Content.ReadFromJsonAsync<ProductSummary>();
        summary!.Sku.Should().Be("BOOK-001");
        summary.Name.Should().Be("Clean Architecture");
        summary.Price.Should().Be(45.90m);
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateSku_ReturnsConflict()
    {
        await _client.PostAsJsonAsync("/api/products/", new CreateProductRequest("dup-001", "Producto", "Desc", "Cat", 10m));

        var response = await _client.PostAsJsonAsync("/api/products/", new CreateProductRequest("DUP-001", "Otro", "Desc", "Cat", 20m));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetById_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListProducts_FiltersByCategory()
    {
        var category = $"Category-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/products/", new CreateProductRequest($"SKU-{Guid.NewGuid():N}", "En la categoría", "Desc", category, 10m));
        await _client.PostAsJsonAsync("/api/products/", new CreateProductRequest($"SKU-{Guid.NewGuid():N}", "En otra categoría", "Desc", "Otra", 10m));

        var response = await _client.GetAsync($"/api/products/?category={category}&page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<ProductSummary>>();
        results.Should().ContainSingle().Which.Category.Should().Be(category);
    }
}
