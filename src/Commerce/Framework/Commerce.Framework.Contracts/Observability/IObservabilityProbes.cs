namespace Commerce.Framework.Contracts.Observability;

public sealed record SchedulingHealthSnapshot(
    bool ProcessorRunning,
    DateTime? LastCycleUtc,
    int PendingJobCount,
    int DeadLetterCount,
    int StaleLockCount);

public interface ISchedulingHealthProbe
{
    Task<SchedulingHealthSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public sealed record PaymentProviderHealthEntry(
    string ProviderSystemName,
    bool IsRegistered,
    bool IsConfigured,
    string? Message);

public interface IPaymentProviderHealthProbe
{
    Task<IReadOnlyList<PaymentProviderHealthEntry>> GetProviderHealthAsync(CancellationToken cancellationToken = default);
}

public interface ICacheHealthProbe
{
    Task<(bool IsHealthy, string Description)> CheckAsync(CancellationToken cancellationToken = default);
}

public sealed record BackupHealthSnapshot(
    bool BackupFresh,
    bool RestoreTested,
    DateTime? LatestBackupAtUtc,
    string? LatestValidityStatus,
    string Message);

public interface IBackupHealthProbe
{
    Task<BackupHealthSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
