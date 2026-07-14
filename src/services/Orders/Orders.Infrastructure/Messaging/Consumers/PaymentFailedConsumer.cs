using ECommerce.Contracts.IntegrationEvents;
using MassTransit;
using MediatR;
using Orders.Application.Orders.Commands.CancelOrder;

namespace Orders.Infrastructure.Messaging.Consumers;

public class PaymentFailedConsumer(ISender sender) : IConsumer<PaymentFailed>
{
    public Task Consume(ConsumeContext<PaymentFailed> context)
        => sender.Send(new CancelOrderCommand(context.Message.OrderId, context.Message.Reason), context.CancellationToken);
}
