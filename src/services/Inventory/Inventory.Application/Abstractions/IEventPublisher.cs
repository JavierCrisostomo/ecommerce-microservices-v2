using ECommerce.Contracts.IntegrationEvents;

namespace Inventory.Application.Abstractions;

// Duplicada a propósito en cada servicio: son microservicios independientes,
// no comparten código de Application entre sí.
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent;
}
