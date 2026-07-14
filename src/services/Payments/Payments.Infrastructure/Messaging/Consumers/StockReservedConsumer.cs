using ECommerce.Contracts.IntegrationEvents;
using MassTransit;
using MediatR;
using Payments.Application.Payments.Commands.ProcessPayment;

namespace Payments.Infrastructure.Messaging.Consumers;

public class StockReservedConsumer(ISender sender) : IConsumer<StockReserved>
{
    public Task Consume(ConsumeContext<StockReserved> context)
        => sender.Send(
            new ProcessPaymentCommand(context.Message.OrderId, context.Message.TotalAmount),
            context.CancellationToken);
}
