using ECommerce.Contracts.IntegrationEvents;
using MassTransit;
using MediatR;
using Orders.Application.Orders.Commands.CancelOrder;

namespace Orders.Infrastructure.Messaging.Consumers;

public class StockRejectedConsumer(ISender sender) : IConsumer<StockRejected>
{
    public Task Consume(ConsumeContext<StockRejected> context)
        => sender.Send(new CancelOrderCommand(context.Message.OrderId, context.Message.Reason), context.CancellationToken);
}
