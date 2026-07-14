namespace ECommerce.Contracts.IntegrationEvents;

public record PaymentCompleted(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid OrderId,
    Guid PaymentId,
    decimal Amount) : IIntegrationEvent;

public record PaymentFailed(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid OrderId,
    string Reason) : IIntegrationEvent;
