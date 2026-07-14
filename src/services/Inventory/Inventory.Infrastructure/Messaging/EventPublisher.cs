using ECommerce.Contracts.IntegrationEvents;
using Inventory.Application.Abstractions;
using MassTransit;

namespace Inventory.Infrastructure.Messaging;

public class EventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent
        => publishEndpoint.Publish(integrationEvent, cancellationToken);
}
