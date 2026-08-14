using Commerce.Framework.Core.Events;
using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Events;

public sealed class DomainEventDispatcher(
    IEventBus eventBus,
    IEnumerable<IDomainEventIntegrationMapper> mappers,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var integrationEvents = mappers
                .SelectMany(mapper => mapper.Map(domainEvent))
                .ToList();

            if (integrationEvents.Count == 0)
            {
                logger.LogDebug(
                    "Domain event {EventType} has no integration event mapping.",
                    domainEvent.GetType().Name);
                continue;
            }

            foreach (var integrationEvent in integrationEvents)
            {
                await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

public interface IDomainEventIntegrationMapper
{
    IEnumerable<IIntegrationEvent> Map(IDomainEvent domainEvent);
}
