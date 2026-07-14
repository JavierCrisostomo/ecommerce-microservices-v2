using Orders.Domain.Entities;
using Orders.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Orders.Infrastructure.Persistence;

public class OrderRepository(OrdersDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
        => dbContext.Orders
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await dbContext.Orders.AddAsync(order, cancellationToken);
    }
}
