using FluentAssertions;
using Notifications.Domain.Entities;

namespace Notifications.Tests.Domain;

public class NotificationTests
{
    [Fact]
    public void Create_WithValidData_SetsAllFields()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        var notification = Notification.Create(orderId, customerId, NotificationType.OrderConfirmed, "Tu pedido fue confirmado");

        notification.OrderId.Should().Be(orderId);
        notification.CustomerId.Should().Be(customerId);
        notification.Type.Should().Be(NotificationType.OrderConfirmed);
        notification.Message.Should().Be("Tu pedido fue confirmado");
        notification.SentAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutMessage_Throws(string message)
    {
        var act = () => Notification.Create(Guid.NewGuid(), Guid.NewGuid(), NotificationType.OrderCancelled, message);

        act.Should().Throw<ArgumentException>();
    }
}
