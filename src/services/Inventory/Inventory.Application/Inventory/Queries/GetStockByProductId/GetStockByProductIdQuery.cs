using MediatR;

namespace Inventory.Application.Inventory.Queries.GetStockByProductId;

public record GetStockByProductIdQuery(Guid ProductId) : IRequest<StockSummary?>;
