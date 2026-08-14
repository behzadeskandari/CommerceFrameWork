using Commerce.Framework.Contracts.Installation;
using Commerce.Framework.Scheduling;
using Commerce.Scheduling.Application.Abstractions;
using Commerce.Scheduling.Application.Execution;
using Commerce.Scheduling.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Commerce.Scheduling.Application.Processing;

public sealed class BackgroundJobProcessorOptions
{
    public int PollIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 20;

    public TimeSpan JobLockDuration { get; set; } = TimeSpan.FromMinutes(5);
}

public sealed class BackgroundJobProcessorState
{
    public DateTime? LastSuccessfulCycleUtc { get; set; }
}

public sealed class BackgroundJobProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobProcessorOptions> options,
    BackgroundJobProcessorState processorState,
    ILogger<BackgroundJobProcessor> logger) : BackgroundService
{
    private readonly BackgroundJobProcessorOptions _options = options.Value;
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCycleAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Background job processor cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var installationState = scope.ServiceProvider.GetRequiredService<IInstallationStateService>();
        if (!await installationState.IsInstalledAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var repository = scope.ServiceProvider.GetRequiredService<ISchedulingRepository>();
        var executor = scope.ServiceProvider.GetRequiredService<BackgroundJobExecutor>();
        var utcNow = DateTime.UtcNow;

        await repository.CleanupExpiredLocksAsync(utcNow, cancellationToken).ConfigureAwait(false);
        await EnqueueDueRecurringJobsAsync(repository, utcNow, cancellationToken).ConfigureAwait(false);

        var dueJobs = await repository.ListDueJobsAsync(utcNow, _options.BatchSize, cancellationToken).ConfigureAwait(false);
        foreach (var job in dueJobs)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var lockUntil = utcNow.Add(_options.JobLockDuration);
            var claimed = await repository.TryClaimJobAsync(job.Id, _instanceId, lockUntil, utcNow, cancellationToken).ConfigureAwait(false);
            if (!claimed)
            {
                continue;
            }

            var claimedJob = await repository.GetJobByIdAsync(job.Id, cancellationToken).ConfigureAwait(false);
            if (claimedJob is null)
            {
                continue;
            }

            await executor.ExecuteAsync(claimedJob, _instanceId, cancellationToken).ConfigureAwait(false);
        }

        processorState.LastSuccessfulCycleUtc = DateTime.UtcNow;
    }

    private static async Task EnqueueDueRecurringJobsAsync(
        ISchedulingRepository repository,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var schedules = await repository.ListDueRecurringAsync(utcNow, cancellationToken).ConfigureAwait(false);
        foreach (var schedule in schedules)
        {
            var lockKey = $"recurring:{schedule.ScheduleKey}";
            var lockOwner = Guid.NewGuid().ToString("N");
            var acquired = await repository
                .TryAcquireDistributedLockAsync(lockKey, lockOwner, utcNow.AddMinutes(1), cancellationToken)
                .ConfigureAwait(false);

            if (!acquired)
            {
                continue;
            }

            try
            {
                var idempotencyKey = $"{schedule.ScheduleKey}:{schedule.NextRunAtUtc:O}";
                var job = BackgroundJob.CreateFromRecurring(
                    schedule.JobType,
                    schedule.ScheduleKey,
                    schedule.PayloadJson,
                    priority: 0,
                    schedule.MaxAttempts,
                    idempotencyKey);

                var existing = await repository
                    .GetJobByIdempotencyKeyAsync(idempotencyKey, cancellationToken)
                    .ConfigureAwait(false);

                if (existing is null)
                {
                    await repository.AddJobAsync(job, cancellationToken).ConfigureAwait(false);
                }

                schedule.MarkEnqueued(utcNow);
                await repository.SaveRecurringAsync(schedule, cancellationToken).ConfigureAwait(false);
                await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await repository.ReleaseDistributedLockAsync(lockKey, lockOwner, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
