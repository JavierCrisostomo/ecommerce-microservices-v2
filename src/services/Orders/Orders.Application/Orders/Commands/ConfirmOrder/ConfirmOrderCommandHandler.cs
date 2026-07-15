using ECommerce.Contracts.IntegrationEvents;
using MediatR;
using Orders.Application.Abstractions;
using Orders.Domain.Repositories;

namespace Orders.Application.Orders.Commands.ConfirmOrder;

public class ConfirmOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrderReadStore orderReadStore,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher) : IRequestHandler<ConfirmOrderCommand>
{
    public async Task Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
            return;

        order.Confirm();

        await eventPublisher.PublishAsync(
            new OrderConfirmed(Guid.NewGuid(), DateTimeOffset.UtcNow, order.Id, order.CustomerId),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await orderReadStore.UpsertAsync(OrderSummaryMapper.ToSummary(order), cancellationToken);
    }
}
