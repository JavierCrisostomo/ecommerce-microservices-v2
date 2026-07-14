using Inventory.Domain.Entities;

namespace Inventory.Domain.Repositories;

public interface IOrderReservationRepository
{
    Task<OrderReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddAsync(OrderReservation reservation, CancellationToken cancellationToken);
}
