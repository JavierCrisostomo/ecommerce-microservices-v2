using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Notifications.Application.Abstractions;
using Notifications.Application.Notifications.Commands.SendNotification;
using Notifications.Domain.Entities;
using Notifications.Domain.Repositories;

namespace Notifications.Tests.Application.Notifications.Commands.SendNotification;

public class SendNotificationCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<SendNotificationCommandHandler>> _logger = new();
    private readonly SendNotificationCommandHandler _handler;

    public SendNotificationCommandHandlerTests()
    {
        _handler = new SendNotificationCommandHandler(_notificationRepository.Object, _unitOfWork.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_PersistsNotificationWithGivenData()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var command = new SendNotificationCommand(orderId, customerId, NotificationType.OrderConfirmed, "Tu pedido fue confirmado");

        await _handler.Handle(command, CancellationToken.None);

        _notificationRepository.Verify(r => r.AddAsync(
            It.Is<Notification>(n =>
                n.OrderId == orderId &&
                n.CustomerId == customerId &&
                n.Type == NotificationType.OrderConfirmed &&
                n.Message == "Tu pedido fue confirmado"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
