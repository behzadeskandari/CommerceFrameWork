using Commerce.Notifications.Application.Abstractions;
using Commerce.Notifications.Application.Templates;
using Commerce.Notifications.Contracts.Dispatch;
using Commerce.Notifications.Domain.Entities;
using Commerce.Notifications.Domain.Enums;
using Commerce.Framework.Application.Observability;
using Commerce.Framework.Contracts.Observability;
using Commerce.Framework.Scheduling;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Commerce.Notifications.Application.Dispatch;

public sealed class NotificationDispatcher(
    INotificationsRepository repository,
    IEnumerable<INotificationChannelProvider> channelProviders,
    IBackgroundJobScheduler jobScheduler,
    ICorrelationContext correlationContext,
    ILogger<NotificationDispatcher> logger)
{
    private readonly IReadOnlyDictionary<NotificationChannel, INotificationChannelProvider> _providers =
        channelProviders.ToDictionary(x => x.Channel);

    public async Task DispatchEventAsync(NotificationEventRequest request, CancellationToken cancellationToken)
    {
        using (CommerceLogging.BeginOperationScope(
            logger,
            correlationContext,
            "notification.dispatch",
            ("EventType", request.EventType),
            ("StoreId", request.StoreId),
            ("CustomerId", request.CustomerId)))
        {
            CommerceMetrics.NotificationOperations.Add(1, new KeyValuePair<string, object?>("operation", "dispatch"));
            await DispatchEventCoreAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchEventCoreAsync(NotificationEventRequest request, CancellationToken cancellationToken)
    {
        var templates = await repository
            .GetEnabledTemplatesForEventAsync(request.EventType, request.StoreId, request.LanguageId, cancellationToken)
            .ConfigureAwait(false);

        var selected = NotificationTemplateSelector.Select(templates, request.StoreId, request.LanguageId);
        if (selected.Count == 0)
        {
            logger.LogDebug("No enabled templates for event {EventType}.", request.EventType);
            return;
        }

        foreach (var template in selected)
        {
            var recipient = ResolveRecipient(template.Channel, request);
            if (string.IsNullOrWhiteSpace(recipient))
            {
                logger.LogWarning(
                    "Skipping {EventType} on {Channel}: recipient unavailable.",
                    request.EventType,
                    template.Channel);
                continue;
            }

            var subject = NotificationTemplateRenderer.Render(template.Subject, request.Variables);
            var body = NotificationTemplateRenderer.Render(template.Body, request.Variables);

            var log = NotificationLog.CreatePending(
                template.Id,
                request.EventType,
                template.Channel,
                request.StoreId,
                request.CustomerId,
                recipient,
                subject,
                body);

            await repository.AddLogAsync(log, cancellationToken).ConfigureAwait(false);
            await DeliverAsync(log, template.Channel, recipient, subject, body, cancellationToken).ConfigureAwait(false);
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RetryLogAsync(NotificationLog log, CancellationToken cancellationToken)
    {
        if (!_providers.TryGetValue(log.Channel, out var provider))
        {
            log.MarkCancelled("Channel provider not registered.");
            await repository.SaveLogAsync(log, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        log.RecordAttempt();
        var result = await provider.SendAsync(
            new NotificationDeliveryRequest(log.Channel, log.Recipient, log.Subject, log.Body, log.Channel is NotificationChannel.Email),
            cancellationToken).ConfigureAwait(false);

        if (result.Success)
        {
            log.MarkSent();
        }
        else
        {
            var nextRetry = CalculateNextRetry(log.AttemptCount);
            log.MarkFailed(result.ErrorMessage ?? "Delivery failed.", nextRetry, incrementAttempt: false);
            await ScheduleDeliveryRetryAsync(log, nextRetry, cancellationToken).ConfigureAwait(false);
        }

        await repository.SaveLogAsync(log, cancellationToken).ConfigureAwait(false);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DeliverAsync(
        NotificationLog log,
        NotificationChannel channel,
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        if (!_providers.TryGetValue(channel, out var provider))
        {
            log.MarkFailed("Channel provider not registered.", null);
            await repository.SaveLogAsync(log, cancellationToken).ConfigureAwait(false);
            return;
        }

        log.RecordAttempt();
        var result = await provider.SendAsync(
            new NotificationDeliveryRequest(channel, recipient, subject, body, channel is NotificationChannel.Email),
            cancellationToken).ConfigureAwait(false);

        if (result.Success)
        {
            log.MarkSent();
        }
        else
        {
            var nextRetry = CalculateNextRetry(log.AttemptCount);
            log.MarkFailed(result.ErrorMessage ?? "Delivery failed.", nextRetry, incrementAttempt: false);
            await ScheduleDeliveryRetryAsync(log, nextRetry, cancellationToken).ConfigureAwait(false);
        }

        await repository.SaveLogAsync(log, cancellationToken).ConfigureAwait(false);
    }

    private async Task ScheduleDeliveryRetryAsync(
        NotificationLog log,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken)
    {
        if (!nextRetryAtUtc.HasValue)
        {
            return;
        }

        var delay = nextRetryAtUtc.Value - DateTime.UtcNow;
        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        var payload = JsonSerializer.Serialize(new { logId = log.Id });
        await jobScheduler.EnqueueDelayedAsync(
            BackgroundJobTypes.NotificationDeliver,
            delay,
            payload,
            idempotencyKey: $"notification-deliver:{log.Id}:{log.AttemptCount}",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    internal static DateTime? CalculateNextRetry(int attemptCount) =>
        attemptCount >= 3 ? null : DateTime.UtcNow.AddMinutes(Math.Pow(2, attemptCount));

    private static string? ResolveRecipient(NotificationChannel channel, NotificationEventRequest request) =>
        channel switch
        {
            NotificationChannel.Email => request.RecipientEmail,
            NotificationChannel.Sms => request.RecipientPhone,
            NotificationChannel.InApp => request.CustomerId?.ToString(),
            _ => null
        };
}

public sealed class NotificationEventPublisher(
    NotificationDispatcher dispatcher,
    ILogger<NotificationEventPublisher> logger) : INotificationEventPublisher
{
    public async Task PublishAsync(NotificationEventRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await dispatcher.DispatchEventAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification dispatch failed for event {EventType}.", request.EventType);
        }
    }
}
