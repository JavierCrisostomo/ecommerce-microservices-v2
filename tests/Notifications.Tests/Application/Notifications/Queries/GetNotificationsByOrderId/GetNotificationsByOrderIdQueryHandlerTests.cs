using FluentAssertions;
using Moq;
using Notifications.Application.Notifications.Queries.GetNotificationsByOrderId;
using Notifications.Domain.Entities;
using Notifications.Domain.Repositories;

namespace Notifications.Tests.Application.Notifications.Queries.GetNotificationsByOrderId;

public class GetNotificationsByOrderIdQueryHandlerTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly GetNotificationsByOrderIdQueryHandler _handler;

    public GetNotificationsByOrderIdQueryHandlerTests()
    {
        _handler = new GetNotificationsByOrderIdQueryHandler(_notificationRepository.Object);
    }

    [Fact]
    public async Task Handle_MapsRepositoryResultsToSummaries()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var notification = Notification.Create(orderId, customerId, NotificationType.OrderConfirmed, "Tu pedido fue confirmado");

        _notificationRepository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([notification]);

        var result = await _handler.Handle(new GetNotificationsByOrderIdQuery(orderId), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].OrderId.Should().Be(orderId);
        result[0].CustomerId.Should().Be(customerId);
        result[0].Type.Should().Be(nameof(NotificationType.OrderConfirmed));
        result[0].Message.Should().Be("Tu pedido fue confirmado");
    }

    [Fact]
    public async Task Handle_WhenNoNotifications_ReturnsEmptyList()
    {
        var orderId = Guid.NewGuid();
        _notificationRepository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _handler.Handle(new GetNotificationsByOrderIdQuery(orderId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
