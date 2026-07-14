using System.Net;
using System.Net.Http.Json;
using ECommerce.Contracts.IntegrationEvents;
using FluentAssertions;
using Inventory.Api.Contracts;
using Inventory.Application.Inventory;
using Inventory.Application.Inventory.Commands.CreateInventoryItem;
using IntegrationTests.Shared;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Inventory.IntegrationTests;

public class InventoryEndpointsTests : IClassFixture<SqlServerAndRabbitMqFixture>, IAsyncLifetime
{
    private readonly InventoryApiFactory _factory;
    private HttpClient _client = null!;

    public InventoryEndpointsTests(SqlServerAndRabbitMqFixture fixture)
    {
        _factory = new InventoryApiFactory(
            fixture.GetSqlConnectionString("inventory_db_it"),
            fixture.RabbitMqHost,
            fixture.RabbitMqPort);
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
    public async Task CreateInventoryItem_ThenGetStock_ReturnsSameQuantity()
    {
        var productId = Guid.NewGuid();

        var createResponse = await _client.PostAsJsonAsync("/api/inventory/", new CreateInventoryItemRequest(productId, 15));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateInventoryItemResult>();
        created!.AvailableQuantity.Should().Be(15);

        var getResponse = await _client.GetAsync($"/api/inventory/{productId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var stock = await getResponse.Content.ReadFromJsonAsync<StockSummary>();
        stock!.AvailableQuantity.Should().Be(15);
    }

    [Fact]
    public async Task CreateInventoryItem_WhenProductAlreadyHasOne_ReturnsConflict()
    {
        var productId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/inventory/", new CreateInventoryItemRequest(productId, 10));

        var response = await _client.PostAsJsonAsync("/api/inventory/", new CreateInventoryItemRequest(productId, 5));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetStock_WhenProductHasNoItem_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/inventory/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OrderCreated_WhenStockIsSufficient_ReservesStockThroughRealMessaging()
    {
        var productId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/inventory/", new CreateInventoryItemRequest(productId, 10));

        using var scope = _factory.Services.CreateScope();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await publishEndpoint.Publish(new OrderCreated(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new OrderLine(productId, 4, 10m)],
            40m));

        StockSummary? stock = null;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var response = await _client.GetAsync($"/api/inventory/{productId}");
            stock = await response.Content.ReadFromJsonAsync<StockSummary>();
            if (stock!.AvailableQuantity == 6)
                break;

            await Task.Delay(300);
        }

        stock!.AvailableQuantity.Should().Be(6);
    }
}
