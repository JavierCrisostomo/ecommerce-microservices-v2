using Inventory.Domain.Entities;

namespace Inventory.Domain.Repositories;

public interface IInventoryItemRepository
{
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<List<InventoryItem>> GetByProductIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken);

    Task AddAsync(InventoryItem item, CancellationToken cancellationToken);
}
