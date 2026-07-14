namespace Inventory.Domain.Entities;

// Registra qué se reservó para un pedido, para poder liberar el stock si el
// pago falla más adelante, y para no reservar dos veces si el mensaje
// OrderCreated se entrega más de una vez (entrega "al menos una vez").
public class OrderReservation
{
    private readonly List<ReservationLine> _lines = [];

    public Guid OrderId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<ReservationLine> Lines => _lines.AsReadOnly();

    private OrderReservation()
    {
    }

    public static OrderReservation Create(Guid orderId, IReadOnlyCollection<NewReservationLine> lines)
    {
        var reservation = new OrderReservation
        {
            OrderId = orderId,
            Status = ReservationStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var line in lines)
            reservation._lines.Add(ReservationLine.Create(line.ProductId, line.Quantity));

        return reservation;
    }

    // Idempotente: si ya se liberó (o el mensaje que dispara esto llega dos veces), no hace nada.
    public bool Release()
    {
        if (Status == ReservationStatus.Released)
            return false;

        Status = ReservationStatus.Released;
        return true;
    }
}
