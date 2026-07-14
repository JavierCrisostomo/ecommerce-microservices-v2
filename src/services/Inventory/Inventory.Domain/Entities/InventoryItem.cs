namespace Inventory.Domain.Entities;

public class InventoryItem
{
    public Guid ProductId { get; private set; }
    public int AvailableQuantity { get; private set; }

    private InventoryItem()
    {
    }

    public static InventoryItem Create(Guid productId, int initialQuantity)
    {
        if (initialQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialQuantity), "La cantidad inicial no puede ser negativa.");

        return new InventoryItem
        {
            ProductId = productId,
            AvailableQuantity = initialQuantity
        };
    }

    public bool TryReserve(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad a reservar debe ser mayor a cero.");

        if (AvailableQuantity < quantity)
            return false;

        AvailableQuantity -= quantity;
        return true;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad a liberar debe ser mayor a cero.");

        AvailableQuantity += quantity;
    }
}
