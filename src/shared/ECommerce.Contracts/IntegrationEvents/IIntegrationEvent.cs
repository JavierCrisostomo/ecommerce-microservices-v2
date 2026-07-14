namespace ECommerce.Contracts.IntegrationEvents;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
}
