using Commerce.Framework.Contracts.Observability;

namespace Commerce.Framework.Infrastructure.Observability;

public sealed class NullCorrelationContext : ICorrelationContext
{
    public string? CorrelationId => null;

    public string? RequestId => null;

    public string? TraceId => null;
}

public sealed class NullSchedulingHealthProbe : ISchedulingHealthProbe
{
    public Task<SchedulingHealthSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new SchedulingHealthSnapshot(false, null, 0, 0, 0));
}

public sealed class NullPaymentProviderHealthProbe : IPaymentProviderHealthProbe
{
    public Task<IReadOnlyList<PaymentProviderHealthEntry>> GetProviderHealthAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PaymentProviderHealthEntry>>(Array.Empty<PaymentProviderHealthEntry>());
}

public sealed class NullCacheHealthProbe : ICacheHealthProbe
{
    public Task<(bool IsHealthy, string Description)> CheckAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult((true, "Cache not configured."));
}

public sealed class NullBackupHealthProbe : IBackupHealthProbe
{
    public Task<BackupHealthSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new BackupHealthSnapshot(false, false, null, null, "Disaster recovery module is not enabled."));
}
