using ECommerce.Contracts.IntegrationEvents;
using Inventory.Application.Inventory.Commands.ReleaseStock;
using MassTransit;
using MediatR;

namespace Inventory.Infrastructure.Messaging.Consumers;

public class PaymentFailedConsumer(ISender sender) : IConsumer<PaymentFailed>
{
    public Task Consume(ConsumeContext<PaymentFailed> context)
        => sender.Send(new ReleaseStockCommand(context.Message.OrderId), context.CancellationToken);
}
