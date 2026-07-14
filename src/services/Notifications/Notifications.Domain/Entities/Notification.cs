namespace Notifications.Domain.Entities;

// El servicio no tiene estado de negocio propio: este registro es apenas
// una bitácora de lo que se "envió", útil para verificar la saga end-to-end.
public class Notification
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Message { get; private set; } = default!;
    public DateTimeOffset SentAt { get; private set; }

    private Notification()
    {
    }

    public static Notification Create(Guid orderId, Guid customerId, NotificationType type, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("El mensaje es obligatorio.", nameof(message));

        return new Notification
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CustomerId = customerId,
            Type = type,
            Message = message,
            SentAt = DateTimeOffset.UtcNow
        };
    }
}
