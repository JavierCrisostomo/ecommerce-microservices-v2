namespace Payments.Domain.Entities;

public class Payment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Payment()
    {
    }

    public static Payment Create(Guid orderId, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "El monto a cobrar debe ser mayor a cero.");

        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // Idempotente: StockReserved podría entregarse más de una vez.
    public bool Complete()
    {
        if (Status != PaymentStatus.Pending)
            return false;

        Status = PaymentStatus.Completed;
        return true;
    }

    public bool Fail(string reason)
    {
        if (Status != PaymentStatus.Pending)
            return false;

        Status = PaymentStatus.Failed;
        FailureReason = reason;
        return true;
    }

    // Se usa cuando Inventory rechaza el stock: el pago nunca llega a procesarse.
    public bool Cancel()
    {
        if (Status != PaymentStatus.Pending)
            return false;

        Status = PaymentStatus.Cancelled;
        return true;
    }
}
