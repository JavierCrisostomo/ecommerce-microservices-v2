namespace ECommerce.Contracts.IntegrationEvents;

public record OrderLine(Guid ProductId, int Quantity, decimal UnitPrice);

public record OrderCreated(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid OrderId,
    Guid CustomerId,
    IReadOnlyCollection<OrderLine> Lines,
    decimal TotalAmount) : IIntegrationEvent;

public record OrderConfirmed(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid OrderId,
    Guid CustomerId) : IIntegrationEvent;

public record OrderCancelled(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid OrderId,
    Guid CustomerId,
    string Reason) : IIntegrationEvent;
