using Inventory.Application.Abstractions;
using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Inventory.Commands.ReleaseStock;

public class ReleaseStockCommandHandler(
    IOrderReservationRepository reservationRepository,
    IInventoryItemRepository inventoryItemRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ReleaseStockCommand>
{
    public async Task Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (reservation is null)
            return;

        // Release() es idempotente: si ya se liberó, no vuelve a sumar stock.
        if (!reservation.Release())
            return;

        var productIds = reservation.Lines.Select(l => l.ProductId).ToList();
        var items = await inventoryItemRepository.GetByProductIdsAsync(productIds, cancellationToken);
        var itemsByProduct = items.ToDictionary(i => i.ProductId);

        foreach (var line in reservation.Lines)
            itemsByProduct[line.ProductId].Release(line.Quantity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
