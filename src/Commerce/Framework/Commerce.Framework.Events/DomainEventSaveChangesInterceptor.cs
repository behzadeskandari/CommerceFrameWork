using Commerce.Framework.Core.Entities;
using Commerce.Framework.Core.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Commerce.Framework.Events;

public sealed class DomainEventSaveChangesInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
        }

        var entities = eventData.Context.ChangeTracker
            .Entries<Entity>()
            .Select(x => x.Entity)
            .Where(x => x.GetDomainEvents().Count > 0)
            .ToList();

        var domainEvents = entities
            .SelectMany(x => x.GetDomainEvents())
            .ToList();

        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        if (domainEvents.Count > 0)
        {
            await dispatcher.DispatchAsync(domainEvents, cancellationToken).ConfigureAwait(false);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }
}
