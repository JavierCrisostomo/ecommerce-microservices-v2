namespace Inventory.Api.Contracts;

public record CreateInventoryItemRequest(Guid ProductId, int InitialQuantity);
