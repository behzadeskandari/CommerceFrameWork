using Commerce.Framework.Contracts.Observability;
using Commerce.Framework.Scheduling;
using Commerce.Scheduling.Application.Abstractions;
using Commerce.Scheduling.Application.Processing;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Scheduling.Infrastructure.Health;

public sealed class SchedulingHealthProbe(
    IServiceScopeFactory scopeFactory,
    BackgroundJobProcessorState processorState) : ISchedulingHealthProbe
{
    public async Task<SchedulingHealthSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISchedulingRepository>();
        var utcNow = DateTime.UtcNow;
        var pending = await repository.ListDueJobsAsync(utcNow.AddDays(1), 1000, cancellationToken).ConfigureAwait(false);
        var deadLetter = await repository.ListJobsAsync(BackgroundJobStatus.DeadLetter, null, 1000, cancellationToken).ConfigureAwait(false);

        return new SchedulingHealthSnapshot(
            processorState.LastSuccessfulCycleUtc.HasValue,
            processorState.LastSuccessfulCycleUtc,
            pending.Count,
            deadLetter.Count,
            0);
    }
}
