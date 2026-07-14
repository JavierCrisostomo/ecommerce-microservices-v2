using MediatR;

namespace Inventory.Application.Inventory.Commands.ReserveStock;

public record ReserveStockLine(Guid ProductId, int Quantity);

public record ReserveStockCommand(Guid OrderId, IReadOnlyCollection<ReserveStockLine> Lines, decimal TotalAmount) : IRequest;
