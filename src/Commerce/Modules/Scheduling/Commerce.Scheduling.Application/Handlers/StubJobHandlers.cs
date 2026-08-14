using Commerce.Framework.Scheduling;
using Commerce.Scheduling.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Commerce.Scheduling.Application.Handlers;

public abstract class StubBackgroundJobHandler(ILogger logger) : IBackgroundJobHandler
{
    public abstract string JobType { get; }

    protected abstract string OperationName { get; }

    public Task<BackgroundJobHandleResult> ExecuteAsync(
        BackgroundJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executed stub background job {JobType} ({Operation}) for job {JobId}.", JobType, OperationName, context.JobId);
        return Task.FromResult(new BackgroundJobHandleResult(true));
    }
}

public sealed class EmailSendJobHandler(ILogger<EmailSendJobHandler> logger) : StubBackgroundJobHandler(logger)
{
    public override string JobType => BackgroundJobTypes.EmailSend;
    protected override string OperationName => "email send";
}

public sealed class SmsSendJobHandler(ILogger<SmsSendJobHandler> logger) : StubBackgroundJobHandler(logger)
{
    public override string JobType => BackgroundJobTypes.SmsSend;
    protected override string OperationName => "sms send";
}

public sealed class ReportsGenerateJobHandler(ILogger<ReportsGenerateJobHandler> logger) : StubBackgroundJobHandler(logger)
{
    public override string JobType => BackgroundJobTypes.ReportsGenerate;
    protected override string OperationName => "report generation";
}

public sealed class MaintenanceCleanupJobHandler(ILogger<MaintenanceCleanupJobHandler> logger) : StubBackgroundJobHandler(logger)
{
    public override string JobType => BackgroundJobTypes.MaintenanceCleanup;
    protected override string OperationName => "maintenance cleanup";
}

public sealed class ExpiredDownloadsJobHandler(ILogger<ExpiredDownloadsJobHandler> logger) : StubBackgroundJobHandler(logger)
{
    public override string JobType => BackgroundJobTypes.ExpiredDownloads;
    protected override string OperationName => "expired downloads";
}

public sealed class InventoryTasksJobHandler(ILogger<InventoryTasksJobHandler> logger) : StubBackgroundJobHandler(logger)
{
    public override string JobType => BackgroundJobTypes.InventoryTasks;
    protected override string OperationName => "inventory tasks";
}

public sealed class PromotionsTasksJobHandler(ILogger<PromotionsTasksJobHandler> logger) : StubBackgroundJobHandler(logger)
{
    public override string JobType => BackgroundJobTypes.PromotionsTasks;
    protected override string OperationName => "promotions tasks";
}

public sealed class PluginTasksJobHandler(ILogger<PluginTasksJobHandler> logger) : StubBackgroundJobHandler(logger)
{
    public override string JobType => BackgroundJobTypes.PluginTasks;
    protected override string OperationName => "plugin tasks";
}

public sealed class DatabaseJobLockProvider(ISchedulingRepository repository) : IJobLockProvider
{
    public async Task<IJobLockHandle?> TryAcquireAsync(string lockKey, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var ownerId = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.Add(duration);
        var acquired = await repository.TryAcquireDistributedLockAsync(lockKey, ownerId, expiresAt, cancellationToken).ConfigureAwait(false);
        return acquired ? new DatabaseJobLockHandle(repository, lockKey, ownerId) : null;
    }

    private sealed class DatabaseJobLockHandle(ISchedulingRepository repository, string lockKey, string ownerId) : IJobLockHandle
    {
        public string LockKey { get; } = lockKey;

        public async ValueTask DisposeAsync()
        {
            await repository.ReleaseDistributedLockAsync(LockKey, ownerId, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
