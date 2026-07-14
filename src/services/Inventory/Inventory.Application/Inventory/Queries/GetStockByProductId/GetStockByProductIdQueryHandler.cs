using Inventory.Domain.Repositories;
using MediatR;

namespace Inventory.Application.Inventory.Queries.GetStockByProductId;

public class GetStockByProductIdQueryHandler(IInventoryItemRepository inventoryItemRepository)
    : IRequestHandler<GetStockByProductIdQuery, StockSummary?>
{
    public async Task<StockSummary?> Handle(GetStockByProductIdQuery request, CancellationToken cancellationToken)
    {
        var item = await inventoryItemRepository.GetByProductIdAsync(request.ProductId, cancellationToken);
        return item is null ? null : new StockSummary(item.ProductId, item.AvailableQuantity);
    }
}
