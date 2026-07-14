using Inventory.Application.Abstractions;

namespace Inventory.Infrastructure.Persistence;

public class UnitOfWork(InventoryDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
