using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Contracts;
using Orders.Application.Orders;
using Orders.Application.Orders.Commands.CreateOrder;
using Xunit;

namespace Orders.IntegrationTests;

public class OrderEndpointsTests : IClassFixture<SqlServerAndRabbitMqFixture>, IAsyncLifetime
{
    private readonly OrdersApiFactory _factory;
    private HttpClient _client = null!;

    public OrderEndpointsTests(SqlServerAndRabbitMqFixture fixture)
    {
        _factory = new OrdersApiFactory(
            fixture.GetSqlConnectionString("orders_db_it"),
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

    private string TokenFor(Guid customerId, string email = "cliente@example.com")
        => JwtTestTokenFactory.Create(_factory.Services.GetRequiredService<IConfiguration>(), customerId, email);

    private HttpClient AuthenticatedClient(Guid customerId, string email = "cliente@example.com")
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(customerId, email));
        return _client;
    }

    [Fact]
    public async Task CreateOrder_WithValidData_PersistsOwnedLinesCorrectly()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var client = AuthenticatedClient(customerId);

        var request = new CreateOrderRequest([new CreateOrderLineDto(productId, "Producto", 10m, 3)]);
        var createResponse = await client.PostAsJsonAsync("/api/orders/", request);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResult>();
        created!.TotalAmount.Should().Be(30m);

        var getResponse = await client.GetAsync($"/api/orders/{created.OrderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await getResponse.Content.ReadFromJsonAsync<OrderSummary>();
        order!.Lines.Should().ContainSingle(l => l.ProductId == productId && l.Quantity == 3 && l.UnitPrice == 10m);
    }

    [Fact]
    public async Task CreateOrder_WithoutToken_ReturnsUnauthorized()
    {
        var request = new CreateOrderRequest([new CreateOrderLineDto(Guid.NewGuid(), "Producto", 10m, 1)]);

        var response = await _client.PostAsJsonAsync("/api/orders/", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_WithNoLines_ReturnsBadRequest()
    {
        var client = AuthenticatedClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/orders/", new CreateOrderRequest([]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrderById_WhenRequestedByAnotherCustomer_ReturnsNotFound()
    {
        var owner = Guid.NewGuid();
        var ownerClient = AuthenticatedClient(owner);
        var created = await (await ownerClient.PostAsJsonAsync("/api/orders/",
            new CreateOrderRequest([new CreateOrderLineDto(Guid.NewGuid(), "Producto", 10m, 1)])))
            .Content.ReadFromJsonAsync<CreateOrderResult>();

        var strangerClient = AuthenticatedClient(Guid.NewGuid());
        var response = await strangerClient.GetAsync($"/api/orders/{created!.OrderId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListOrders_ReturnsOnlyOrdersOfTheCallingCustomer()
    {
        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();

        await AuthenticatedClient(customerA).PostAsJsonAsync("/api/orders/",
            new CreateOrderRequest([new CreateOrderLineDto(Guid.NewGuid(), "De A", 10m, 1)]));
        await AuthenticatedClient(customerB).PostAsJsonAsync("/api/orders/",
            new CreateOrderRequest([new CreateOrderLineDto(Guid.NewGuid(), "De B", 10m, 1)]));

        var response = await AuthenticatedClient(customerA).GetAsync("/api/orders/?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderSummary>>();
        orders.Should().OnlyContain(o => o.CustomerId == customerA);
    }
}
