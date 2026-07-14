namespace Inventory.Domain.Entities;

public class ReservationLine
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    private ReservationLine()
    {
    }

    internal static ReservationLine Create(Guid productId, int quantity) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = productId,
        Quantity = quantity
    };
}
