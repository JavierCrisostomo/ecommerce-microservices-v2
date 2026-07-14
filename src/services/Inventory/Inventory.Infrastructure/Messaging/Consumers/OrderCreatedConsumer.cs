using ECommerce.Contracts.IntegrationEvents;
using Inventory.Application.Inventory.Commands.ReserveStock;
using MassTransit;
using MediatR;

namespace Inventory.Infrastructure.Messaging.Consumers;

public class OrderCreatedConsumer(ISender sender) : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var lines = context.Message.Lines
            .Select(l => new ReserveStockLine(l.ProductId, l.Quantity))
            .ToList();

        await sender.Send(
            new ReserveStockCommand(context.Message.OrderId, lines, context.Message.TotalAmount),
            context.CancellationToken);
    }
}
