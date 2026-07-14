namespace Orders.Domain.Entities;

public class OrderLine
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = default!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    private OrderLine()
    {
    }

    internal static OrderLine Create(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe ser mayor a cero.");

        if (unitPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "El precio unitario debe ser mayor a cero.");

        return new OrderLine
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }
}
