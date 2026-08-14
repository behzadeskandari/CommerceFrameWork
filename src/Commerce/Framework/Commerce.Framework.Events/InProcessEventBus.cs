using Microsoft.Extensions.Logging;

namespace Commerce.Framework.Events;

public sealed class InProcessEventBus(
    IEnumerable<IIntegrationEventHandler> handlers,
    ILogger<InProcessEventBus> logger) : IEventBus
{
    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var matching = handlers
            .Where(x => x.SupportedEventTypes.Contains(integrationEvent.EventType, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (matching.Count == 0)
        {
            logger.LogDebug("No handlers registered for integration event {EventType}.", integrationEvent.EventType);
            return;
        }

        foreach (var handler in matching)
        {
            await handler.HandleAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
