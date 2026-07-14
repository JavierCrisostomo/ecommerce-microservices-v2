using System.Net.Http.Json;
using ECommerce.Contracts.IntegrationEvents;
using FluentAssertions;
using IntegrationTests.Shared;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Notifications;
using Xunit;

namespace Notifications.IntegrationTests;

public class NotificationEndpointsTests : IClassFixture<SqlServerAndRabbitMqFixture>, IAsyncLifetime
{
    private readonly NotificationsApiFactory _factory;
    private HttpClient _client = null!;

    public NotificationEndpointsTests(SqlServerAndRabbitMqFixture fixture)
    {
        _factory = new NotificationsApiFactory(
            fixture.GetSqlConnectionString("notifications_db_it"),
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

    private async Task<List<NotificationSummary>> PollUntilNotEmptyAsync(Guid orderId)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var response = await _client.GetAsync($"/api/notifications/order/{orderId}");
            var notifications = await response.Content.ReadFromJsonAsync<List<NotificationSummary>>();
            if (notifications is { Count: > 0 })
                return notifications;

            await Task.Delay(300);
        }

        return [];
    }

    [Fact]
    public async Task GetNotifications_WhenNoneRecorded_ReturnsEmptyList()
    {
        var response = await _client.GetAsync($"/api/notifications/order/{Guid.NewGuid()}");

        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationSummary>>();
        notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task OrderConfirmed_RecordsAConfirmationNotification()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            await publishEndpoint.Publish(new OrderConfirmed(Guid.NewGuid(), DateTimeOffset.UtcNow, orderId, customerId));
        }

        var notifications = await PollUntilNotEmptyAsync(orderId);

        notifications.Should().ContainSingle();
        notifications[0].CustomerId.Should().Be(customerId);
        notifications[0].Type.Should().Be("OrderConfirmed");
    }

    [Fact]
    public async Task OrderCancelled_RecordsACancellationNotificationWithReason()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            await publishEndpoint.Publish(new OrderCancelled(Guid.NewGuid(), DateTimeOffset.UtcNow, orderId, customerId, "Stock insuficiente"));
        }

        var notifications = await PollUntilNotEmptyAsync(orderId);

        notifications.Should().ContainSingle();
        notifications[0].Type.Should().Be("OrderCancelled");
        notifications[0].Message.Should().Contain("Stock insuficiente");
    }
}
