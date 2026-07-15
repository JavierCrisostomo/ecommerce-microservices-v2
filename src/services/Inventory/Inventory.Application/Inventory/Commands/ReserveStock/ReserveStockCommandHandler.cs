using ECommerce.Contracts.IntegrationEvents;
using Inventory.Application.Abstractions;
using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Inventory.Commands.ReserveStock;

public class ReserveStockCommandHandler(
    IOrderReservationRepository reservationRepository,
    IInventoryItemRepository inventoryItemRepository,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher) : IRequestHandler<ReserveStockCommand>
{
    public async Task Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        // Idempotencia: OrderCreated puede entregarse más de una vez (at-least-once).
        var existingReservation = await reservationRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existingReservation is not null)
            return;

        var productIds = request.Lines.Select(l => l.ProductId).ToList();
        var items = await inventoryItemRepository.GetByProductIdsAsync(productIds, cancellationToken);
        var itemsByProduct = items.ToDictionary(i => i.ProductId);

        var canReserveAll = request.Lines.All(line =>
            itemsByProduct.TryGetValue(line.ProductId, out var item) && item.AvailableQuantity >= line.Quantity);

        if (!canReserveAll)
        {
            await eventPublisher.PublishAsync(
                new StockRejected(
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    request.OrderId,
                    "Stock insuficiente para uno o más productos del pedido."),
                cancellationToken);
            return;
        }

        // Todo o nada: recién acá se muta, una vez confirmado que alcanza para todas las líneas.
        foreach (var line in request.Lines)
            itemsByProduct[line.ProductId].TryReserve(line.Quantity);

        var reservation = OrderReservation.Create(
            request.OrderId,
            request.Lines.Select(l => new NewReservationLine(l.ProductId, l.Quantity)).ToList());

        await reservationRepository.AddAsync(reservation, cancellationToken);

        await eventPublisher.PublishAsync(
            new StockReserved(Guid.NewGuid(), DateTimeOffset.UtcNow, request.OrderId, request.TotalAmount),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
