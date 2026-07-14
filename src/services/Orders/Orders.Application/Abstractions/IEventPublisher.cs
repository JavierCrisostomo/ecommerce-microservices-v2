using ECommerce.Contracts.IntegrationEvents;

namespace Orders.Application.Abstractions;

// Pequeña abstracción a propósito duplicada por servicio: cada microservicio
// es independiente y no debe compartir código de Application con los demás.
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent;
}
