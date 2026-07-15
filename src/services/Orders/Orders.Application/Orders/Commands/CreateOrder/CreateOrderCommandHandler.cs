using Orders.Application.Abstractions;
using Orders.Domain.Entities;
using Orders.Domain.Repositories;
using MediatR;
using ContractsOrderLine = ECommerce.Contracts.IntegrationEvents.OrderLine;
using OrderCreatedEvent = ECommerce.Contracts.IntegrationEvents.OrderCreated;

namespace Orders.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrderReadStore orderReadStore,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher) : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var newLines = request.Lines
            .Select(l => new NewOrderLine(l.ProductId, l.ProductName, l.UnitPrice, l.Quantity))
            .ToList();

        var order = Order.Create(request.CustomerId, newLines);

        await orderRepository.AddAsync(order, cancellationToken);

        // El evento se publica antes de SaveChanges para que el outbox de MassTransit
        // lo bufferee y lo persista de forma atómica junto con el write model.
        await eventPublisher.PublishAsync(
            new OrderCreatedEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                order.Id,
                order.CustomerId,
                order.Lines.Select(l => new ContractsOrderLine(l.ProductId, l.Quantity, l.UnitPrice)).ToList(),
                order.TotalAmount),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // La proyección de lectura se actualiza sincrónicamente, justo después de
        // confirmar el write model; el resto de la saga sí viaja por RabbitMQ.
        await orderReadStore.UpsertAsync(OrderSummaryMapper.ToSummary(order), cancellationToken);

        return new CreateOrderResult(order.Id, order.TotalAmount, order.Status.ToString());
    }
}
