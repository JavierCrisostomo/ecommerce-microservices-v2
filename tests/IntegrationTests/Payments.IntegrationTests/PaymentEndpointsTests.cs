using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.Contracts.IntegrationEvents;
using FluentAssertions;
using IntegrationTests.Shared;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Payments;
using Xunit;

namespace Payments.IntegrationTests;

public class PaymentEndpointsTests : IClassFixture<SqlServerAndRabbitMqFixture>, IAsyncLifetime
{
    private readonly PaymentsApiFactory _factory;
    private HttpClient _client = null!;

    public PaymentEndpointsTests(SqlServerAndRabbitMqFixture fixture)
    {
        _factory = new PaymentsApiFactory(
            fixture.GetSqlConnectionString("payments_db_it"),
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

    private HttpClient AuthenticatedClient()
    {
        var token = JwtTestTokenFactory.Create(_factory.Services.GetRequiredService<IConfiguration>(), Guid.NewGuid(), "cliente@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client;
    }

    private async Task PublishStockReservedAsync(Guid orderId, decimal totalAmount)
    {
        using var scope = _factory.Services.CreateScope();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await publishEndpoint.Publish(new StockReserved(Guid.NewGuid(), DateTimeOffset.UtcNow, orderId, totalAmount));
    }

    private async Task<PaymentSummary?> PollUntilExistsAsync(Guid orderId)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var response = await AuthenticatedClient().GetAsync($"/api/payments/order/{orderId}");
            if (response.StatusCode == HttpStatusCode.OK)
                return await response.Content.ReadFromJsonAsync<PaymentSummary>();

            await Task.Delay(300);
        }

        return null;
    }

    [Fact]
    public async Task GetPayment_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync($"/api/payments/order/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPayment_WhenNoPaymentForOrder_ReturnsNotFound()
    {
        var response = await AuthenticatedClient().GetAsync($"/api/payments/order/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StockReserved_WithAmountBelowThreshold_CompletesThePayment()
    {
        var orderId = Guid.NewGuid();

        await PublishStockReservedAsync(orderId, 50m);
        var payment = await PollUntilExistsAsync(orderId);

        payment.Should().NotBeNull();
        payment!.Status.Should().Be("Completed");
        payment.Amount.Should().Be(50m);
    }

    [Fact]
    public async Task StockReserved_WithAmountAboveThreshold_FailsThePayment()
    {
        var orderId = Guid.NewGuid();

        await PublishStockReservedAsync(orderId, 1500m);
        var payment = await PollUntilExistsAsync(orderId);

        payment.Should().NotBeNull();
        payment!.Status.Should().Be("Failed");
        payment.FailureReason.Should().NotBeNullOrWhiteSpace();
    }
}
