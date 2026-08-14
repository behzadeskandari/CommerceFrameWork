namespace Commerce.Framework.Events;

public interface IEventBus
{
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}

public interface IIntegrationEventHandler
{
    IReadOnlyCollection<string> SupportedEventTypes { get; }

    Task HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
