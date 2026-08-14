using System.Text.Json;
using Commerce.Framework.Scheduling;
using Commerce.Notifications.Application.Abstractions;
using Commerce.Notifications.Application.Dispatch;
using Microsoft.Extensions.Logging;

namespace Commerce.Notifications.Application.Jobs;

public sealed class NotificationRetryJobHandler(
    INotificationsRepository repository,
    NotificationDispatcher dispatcher,
    ILogger<NotificationRetryJobHandler> logger) : IBackgroundJobHandler
{
    public string JobType => BackgroundJobTypes.NotificationRetry;

    public async Task<BackgroundJobHandleResult> ExecuteAsync(
        BackgroundJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var candidates = await repository
            .GetRetryCandidatesAsync(DateTime.UtcNow, 20, cancellationToken)
            .ConfigureAwait(false);

        foreach (var log in candidates)
        {
            try
            {
                await dispatcher.RetryLogAsync(log, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification retry failed for log {LogId}.", log.Id);
            }
        }

        return new BackgroundJobHandleResult(true);
    }
}

public sealed class NotificationDeliverJobHandler(
    INotificationsRepository repository,
    NotificationDispatcher dispatcher,
    ILogger<NotificationDeliverJobHandler> logger) : IBackgroundJobHandler
{
    public string JobType => BackgroundJobTypes.NotificationDeliver;

    public async Task<BackgroundJobHandleResult> ExecuteAsync(
        BackgroundJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context.PayloadJson))
        {
            return new BackgroundJobHandleResult(false, "Missing notification log payload.");
        }

        NotificationDeliverPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<NotificationDeliverPayload>(context.PayloadJson);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid notification deliver payload for job {JobId}.", context.JobId);
            return new BackgroundJobHandleResult(false, "Invalid payload.");
        }

        if (payload is null || payload.LogId <= 0)
        {
            return new BackgroundJobHandleResult(false, "Notification log id is required.");
        }

        var log = await repository.GetLogByIdAsync(payload.LogId, cancellationToken).ConfigureAwait(false);
        if (log is null)
        {
            return new BackgroundJobHandleResult(true);
        }

        if (log.Status is Commerce.Notifications.Domain.Enums.NotificationDeliveryStatus.Sent)
        {
            return new BackgroundJobHandleResult(true);
        }

        await dispatcher.RetryLogAsync(log, cancellationToken).ConfigureAwait(false);

        if (log.Status is Commerce.Notifications.Domain.Enums.NotificationDeliveryStatus.Sent)
        {
            return new BackgroundJobHandleResult(true);
        }

        return new BackgroundJobHandleResult(
            false,
            log.LastError ?? "Delivery failed.",
            RetryRequested: log.Status is Commerce.Notifications.Domain.Enums.NotificationDeliveryStatus.Pending,
            RetryDelay: TimeSpan.FromMinutes(2));
    }

    private sealed record NotificationDeliverPayload(int LogId);
}
