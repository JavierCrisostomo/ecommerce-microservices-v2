using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public class OrderReservationRepository(InventoryDbContext dbContext) : IOrderReservationRepository
{
    public Task<OrderReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
        => dbContext.OrderReservations
            .Include(r => r.Lines)
            .SingleOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);

    public async Task AddAsync(OrderReservation reservation, CancellationToken cancellationToken)
    {
        await dbContext.OrderReservations.AddAsync(reservation, cancellationToken);
    }
}
