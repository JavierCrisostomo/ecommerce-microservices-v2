using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public class InventoryItemRepository(InventoryDbContext dbContext) : IInventoryItemRepository
{
    public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
        => dbContext.InventoryItems.SingleOrDefaultAsync(i => i.ProductId == productId, cancellationToken);

    public Task<List<InventoryItem>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken)
        => dbContext.InventoryItems.Where(i => productIds.Contains(i.ProductId)).ToListAsync(cancellationToken);

    public async Task AddAsync(InventoryItem item, CancellationToken cancellationToken)
    {
        await dbContext.InventoryItems.AddAsync(item, cancellationToken);
    }
}
