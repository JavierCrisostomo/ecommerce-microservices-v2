using ECommerce.Contracts.IntegrationEvents;
using MassTransit;
using MediatR;
using Orders.Application.Orders.Commands.ConfirmOrder;

namespace Orders.Infrastructure.Messaging.Consumers;

public class PaymentCompletedConsumer(ISender sender) : IConsumer<PaymentCompleted>
{
    public Task Consume(ConsumeContext<PaymentCompleted> context)
        => sender.Send(new ConfirmOrderCommand(context.Message.OrderId), context.CancellationToken);
}
