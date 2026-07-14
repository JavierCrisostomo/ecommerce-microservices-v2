using Orders.Application.Abstractions;

namespace Orders.Infrastructure.Persistence;

public class UnitOfWork(OrdersDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
