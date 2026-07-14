using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Inventory.Api.Contracts;
using Inventory.Application.Inventory;
using Inventory.IntegrationTests;
using IntegrationTests.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Notifications;
using Notifications.IntegrationTests;
using Orders.Api.Contracts;
using Orders.Application.Orders;
using Orders.Application.Orders.Commands.CreateOrder;
using Orders.IntegrationTests;
using Payments.Application.Payments;
using Payments.IntegrationTests;
using Xunit;

namespace Saga.IntegrationTests;

// Los 4 servicios de la saga (Orders, Inventory, Payments, Notifications) corriendo
// de verdad, hablando entre sí por un RabbitMQ real de Testcontainers — sin mockear
// IEventPublisher en ningún lado. Es la versión automatizada de lo que se probó a
// mano con curl durante el desarrollo.
public class FullCheckoutSagaTests : IClassFixture<SqlServerAndRabbitMqFixture>, IAsyncLifetime
{
    private readonly OrdersApiFactory _ordersFactory;
    private readonly InventoryApiFactory _inventoryFactory;
    private readonly PaymentsApiFactory _paymentsFactory;
    private readonly NotificationsApiFactory _notificationsFactory;

    private HttpClient _orders = null!;
    private HttpClient _inventory = null!;
    private HttpClient _payments = null!;
    private HttpClient _notifications = null!;

    public FullCheckoutSagaTests(SqlServerAndRabbitMqFixture fixture)
    {
        _ordersFactory = new OrdersApiFactory(fixture.GetSqlConnectionString("orders_db_saga"), fixture.RabbitMqHost, fixture.RabbitMqPort);
        _inventoryFactory = new InventoryApiFactory(fixture.GetSqlConnectionString("inventory_db_saga"), fixture.RabbitMqHost, fixture.RabbitMqPort);
        _paymentsFactory = new PaymentsApiFactory(fixture.GetSqlConnectionString("payments_db_saga"), fixture.RabbitMqHost, fixture.RabbitMqPort);
        _notificationsFactory = new NotificationsApiFactory(fixture.GetSqlConnectionString("notifications_db_saga"), fixture.RabbitMqHost, fixture.RabbitMqPort);
    }

    public Task InitializeAsync()
    {
        // Instanciar los 4 clientes fuerza a que los 4 hosts arranquen (y sus buses
        // de MassTransit se conecten al RabbitMQ real) antes de correr el test.
        _orders = _ordersFactory.CreateClient();
        _inventory = _inventoryFactory.CreateClient();
        _payments = _paymentsFactory.CreateClient();
        _notifications = _notificationsFactory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _ordersFactory.Dispose();
        _inventoryFactory.Dispose();
        _paymentsFactory.Dispose();
        _notificationsFactory.Dispose();
        return Task.CompletedTask;
    }

    private HttpClient OrdersClientAs(Guid customerId)
    {
        var token = JwtTestTokenFactory.Create(_ordersFactory.Services.GetRequiredService<IConfiguration>(), customerId, "cliente@example.com");
        _orders.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _orders;
    }

    private HttpClient PaymentsClientAs(Guid customerId)
    {
        var token = JwtTestTokenFactory.Create(_paymentsFactory.Services.GetRequiredService<IConfiguration>(), customerId, "cliente@example.com");
        _payments.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _payments;
    }

    private async Task SeedStockAsync(Guid productId, int quantity)
    {
        await _inventory.PostAsJsonAsync("/api/inventory/", new CreateInventoryItemRequest(productId, quantity));
    }

    private async Task<OrderSummary> PollOrderUntilAsync(Guid orderId, params string[] statuses)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var response = await _orders.GetAsync($"/api/orders/{orderId}");
            var order = await response.Content.ReadFromJsonAsync<OrderSummary>();
            if (order is not null && statuses.Contains(order.Status))
                return order;

            await Task.Delay(300);
        }

        throw new TimeoutException($"El pedido {orderId} no llegó a ninguno de los estados [{string.Join(",", statuses)}] a tiempo.");
    }

    [Fact]
    public async Task HappyPath_OrderIsConfirmedPaymentCompletesAndNotificationIsSent()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedStockAsync(productId, 10);

        var client = OrdersClientAs(customerId);
        var createResponse = await client.PostAsJsonAsync("/api/orders/",
            new CreateOrderRequest([new CreateOrderLineDto(productId, "Producto Saga", 20m, 3)]));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResult>();

        var order = await PollOrderUntilAsync(created!.OrderId, "Confirmed", "Cancelled");

        order.Status.Should().Be("Confirmed");

        var paymentResponse = await PaymentsClientAs(customerId).GetAsync($"/api/payments/order/{created.OrderId}");
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentSummary>();
        payment!.Status.Should().Be("Completed");
        payment.Amount.Should().Be(60m);

        var stockResponse = await _inventory.GetAsync($"/api/inventory/{productId}");
        var stock = await stockResponse.Content.ReadFromJsonAsync<StockSummary>();
        stock!.AvailableQuantity.Should().Be(7);

        var notificationsResponse = await _notifications.GetAsync($"/api/notifications/order/{created.OrderId}");
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<List<NotificationSummary>>();
        notifications.Should().ContainSingle(n => n.Type == "OrderConfirmed");
    }

    [Fact]
    public async Task InsufficientStock_CancelsOrderWithoutChargingAndNotifiesCancellation()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedStockAsync(productId, 2);

        var client = OrdersClientAs(customerId);
        var createResponse = await client.PostAsJsonAsync("/api/orders/",
            new CreateOrderRequest([new CreateOrderLineDto(productId, "Producto Escaso", 20m, 5)]));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResult>();

        var order = await PollOrderUntilAsync(created!.OrderId, "Confirmed", "Cancelled");

        order.Status.Should().Be("Cancelled");
        order.CancellationReason.Should().Contain("Stock insuficiente");

        var stockResponse = await _inventory.GetAsync($"/api/inventory/{productId}");
        var stock = await stockResponse.Content.ReadFromJsonAsync<StockSummary>();
        stock!.AvailableQuantity.Should().Be(2); // nunca se tocó

        var paymentResponse = await PaymentsClientAs(customerId).GetAsync($"/api/payments/order/{created.OrderId}");
        paymentResponse.IsSuccessStatusCode.Should().BeFalse("nunca debería haberse creado un pago");

        var notificationsResponse = await _notifications.GetAsync($"/api/notifications/order/{created.OrderId}");
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<List<NotificationSummary>>();
        notifications.Should().ContainSingle(n => n.Type == "OrderCancelled");
    }

    [Fact]
    public async Task PaymentRejected_CancelsOrderAndReleasesStockBack()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await SeedStockAsync(productId, 10);

        var client = OrdersClientAs(customerId);
        // 8 unidades a $200 = $1600, supera el umbral de rechazo simulado (>1000).
        var createResponse = await client.PostAsJsonAsync("/api/orders/",
            new CreateOrderRequest([new CreateOrderLineDto(productId, "Producto Caro", 200m, 8)]));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResult>();

        var order = await PollOrderUntilAsync(created!.OrderId, "Confirmed", "Cancelled");

        order.Status.Should().Be("Cancelled");
        order.CancellationReason.Should().Contain("Pago rechazado");

        var paymentResponse = await PaymentsClientAs(customerId).GetAsync($"/api/payments/order/{created.OrderId}");
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentSummary>();
        payment!.Status.Should().Be("Failed");

        StockSummary? stock = null;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var stockResponse = await _inventory.GetAsync($"/api/inventory/{productId}");
            stock = await stockResponse.Content.ReadFromJsonAsync<StockSummary>();
            if (stock!.AvailableQuantity == 10)
                break;

            await Task.Delay(300);
        }

        stock!.AvailableQuantity.Should().Be(10, "la compensación debería haber liberado el stock reservado");

        var notificationsResponse = await _notifications.GetAsync($"/api/notifications/order/{created.OrderId}");
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<List<NotificationSummary>>();
        notifications.Should().ContainSingle(n => n.Type == "OrderCancelled");
    }
}
