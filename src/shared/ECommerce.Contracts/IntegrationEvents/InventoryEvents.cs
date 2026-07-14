namespace ECommerce.Contracts.IntegrationEvents;

public record StockReserved(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid OrderId,
    decimal TotalAmount) : IIntegrationEvent;

public record StockRejected(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid OrderId,
    string Reason) : IIntegrationEvent;
