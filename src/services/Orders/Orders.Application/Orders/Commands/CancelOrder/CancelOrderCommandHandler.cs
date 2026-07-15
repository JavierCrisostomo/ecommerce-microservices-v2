using ECommerce.Contracts.IntegrationEvents;
using MediatR;
using Orders.Application.Abstractions;
using Orders.Domain.Repositories;

namespace Orders.Application.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrderReadStore orderReadStore,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher) : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return;

        order.Cancel(request.Reason);

        await eventPublisher.PublishAsync(
            new OrderCancelled(Guid.NewGuid(), DateTimeOffset.UtcNow, order.Id, order.CustomerId, request.Reason),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await orderReadStore.UpsertAsync(OrderSummaryMapper.ToSummary(order), cancellationToken);
    }
}
