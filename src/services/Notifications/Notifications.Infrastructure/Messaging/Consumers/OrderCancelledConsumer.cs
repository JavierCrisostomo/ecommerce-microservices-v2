using ECommerce.Contracts.IntegrationEvents;
using MassTransit;
using MediatR;
using Notifications.Application.Notifications.Commands.SendNotification;
using Notifications.Domain.Entities;

namespace Notifications.Infrastructure.Messaging.Consumers;

public class OrderCancelledConsumer(ISender sender) : IConsumer<OrderCancelled>
{
    public Task Consume(ConsumeContext<OrderCancelled> context)
    {
        var message = $"Tu pedido {context.Message.OrderId} fue cancelado: {context.Message.Reason}";

        return sender.Send(
            new SendNotificationCommand(context.Message.OrderId, context.Message.CustomerId, NotificationType.OrderCancelled, message),
            context.CancellationToken);
    }
}
