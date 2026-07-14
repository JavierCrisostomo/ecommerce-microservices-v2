namespace Orders.Domain.Entities;

public class Order
{
    private readonly List<OrderLine> _lines = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    private Order()
    {
    }

    public static Order Create(Guid customerId, IReadOnlyCollection<NewOrderLine> lines)
    {
        if (lines.Count == 0)
            throw new ArgumentException("El pedido debe tener al menos una línea.", nameof(lines));

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var line in lines)
            order._lines.Add(OrderLine.Create(line.ProductId, line.ProductName, line.UnitPrice, line.Quantity));

        order.TotalAmount = order._lines.Sum(l => l.LineTotal);

        return order;
    }

    // Idempotente: la saga puede entregar el evento que dispara esta transición más de una vez.
    public void Confirm()
    {
        if (Status == OrderStatus.Confirmed)
            return;

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"No se puede confirmar un pedido en estado {Status}.");

        Status = OrderStatus.Confirmed;
    }

    // Idempotente por la misma razón. Cubre tanto el rechazo de stock como el rechazo de pago.
    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Cancelled)
            return;

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"No se puede cancelar un pedido en estado {Status}.");

        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
    }
}
