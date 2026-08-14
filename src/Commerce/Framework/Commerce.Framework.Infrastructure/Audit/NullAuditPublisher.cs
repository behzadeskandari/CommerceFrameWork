using Commerce.Framework.Contracts.Audit;

namespace Commerce.Framework.Infrastructure.Audit;

public sealed class NullAuditPublisher : IAuditPublisher
{
    public Task PublishAsync(AuditPublishRequest request, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
