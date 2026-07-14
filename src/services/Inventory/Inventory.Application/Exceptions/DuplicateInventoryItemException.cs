namespace Inventory.Application.Exceptions;

public class DuplicateInventoryItemException(Guid productId)
    : Exception($"Ya existe un ítem de inventario para el producto '{productId}'.");
