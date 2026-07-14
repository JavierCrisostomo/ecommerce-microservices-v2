using ECommerce.Contracts.IntegrationEvents;
using MassTransit;
using MediatR;
using Notifications.Application.Notifications.Commands.SendNotification;
using Notifications.Domain.Entities;

namespace Notifications.Infrastructure.Messaging.Consumers;

public class OrderConfirmedConsumer(ISender sender) : IConsumer<OrderConfirmed>
{
    public Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var message = $"Tu pedido {context.Message.OrderId} fue confirmado. ¡Gracias por tu compra!";

        return sender.Send(
            new SendNotificationCommand(context.Message.OrderId, context.Message.CustomerId, NotificationType.OrderConfirmed, message),
            context.CancellationToken);
    }
}
