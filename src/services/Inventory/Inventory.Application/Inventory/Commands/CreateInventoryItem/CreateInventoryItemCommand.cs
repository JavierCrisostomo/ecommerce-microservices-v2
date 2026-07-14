using MediatR;

namespace Inventory.Application.Inventory.Commands.CreateInventoryItem;

public record CreateInventoryItemCommand(Guid ProductId, int InitialQuantity) : IRequest<CreateInventoryItemResult>;

public record CreateInventoryItemResult(Guid ProductId, int AvailableQuantity);
