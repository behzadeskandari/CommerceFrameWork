using Commerce.Audit.Application.Writing;
using Commerce.Audit.Infrastructure.Security;
using Commerce.Framework.Contracts.Audit;

namespace Commerce.Audit.Infrastructure.Writing;

public sealed class AuditingAuditPublisher(AuditWriter writer, IAuditActorContext actorContext) : IAuditPublisher
{
    public Task PublishAsync(AuditPublishRequest request, CancellationToken cancellationToken = default) =>
        writer.PublishAsync(actorContext.Enrich(request), cancellationToken);
}
