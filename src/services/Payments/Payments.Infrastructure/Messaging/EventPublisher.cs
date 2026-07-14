using ECommerce.Contracts.IntegrationEvents;
using MassTransit;
using Payments.Application.Abstractions;

namespace Payments.Infrastructure.Messaging;

public class EventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent
        => publishEndpoint.Publish(integrationEvent, cancellationToken);
}
