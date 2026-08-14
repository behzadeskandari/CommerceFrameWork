using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Commerce.Integration.Application.Abstractions;
using Commerce.Integration.Contracts.Webhooks;
using Commerce.Integration.Domain.Entities;
using Commerce.Integration.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Commerce.Integration.Application.Webhooks;

public sealed class WebhookDeliveryProcessor(
    IIntegrationRepository repository,
    IWebhookSignatureService signatureService,
    IHttpClientFactory httpClientFactory,
    ILogger<WebhookDeliveryProcessor> logger) : IWebhookDeliveryProcessor
{
    public const int MaxAttempts = 5;

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(4)
    ];

    public async Task ProcessPendingDeliveriesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var pending = await repository.GetPendingDeliveriesAsync(utcNow, 50, cancellationToken).ConfigureAwait(false);

        foreach (var delivery in pending)
        {
            if (!delivery.CanRetry(utcNow, MaxAttempts))
            {
                continue;
            }

            var subscription = await repository
                .GetSubscriptionByIdAsync(delivery.WebhookSubscriptionId, cancellationToken)
                .ConfigureAwait(false);

            if (subscription is null || !subscription.IsActive || subscription.IsDeleted)
            {
                delivery.MarkFailed(null, "Subscription inactive or deleted.", null, MaxAttempts);
                await repository.UpdateDeliveryAsync(delivery, cancellationToken).ConfigureAwait(false);
                continue;
            }

            delivery.MarkDelivering();
            await repository.UpdateDeliveryAsync(delivery, cancellationToken).ConfigureAwait(false);

            try
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var signature = signatureService.ComputeSignature(subscription.Secret, timestamp, delivery.PayloadJson);
                var client = httpClientFactory.CreateClient("Commerce.Webhooks");

                using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Url);
                request.Content = new StringContent(delivery.PayloadJson, Encoding.UTF8, "application/json");
                request.Headers.Add("X-Commerce-Event-Id", delivery.IntegrationEventId.ToString("D"));
                request.Headers.Add("X-Commerce-Event-Type", delivery.EventType);
                request.Headers.Add("X-Commerce-Signature", $"t={timestamp},v1={signature}");
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Commerce-Webhooks", "1.0"));

                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    delivery.MarkSucceeded((int)response.StatusCode);
                    await repository.UpdateDeliveryAsync(delivery, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                ScheduleRetry(
                    delivery,
                    (int)response.StatusCode,
                    $"HTTP {(int)response.StatusCode}: {Truncate(body, 500)}",
                    utcNow);
                await repository.UpdateDeliveryAsync(delivery, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Webhook delivery {DeliveryId} failed.", delivery.Id);
                ScheduleRetry(delivery, null, ex.Message, utcNow);
                await repository.UpdateDeliveryAsync(delivery, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static void ScheduleRetry(WebhookDelivery delivery, int? statusCode, string? error, DateTime utcNow)
    {
        var delayIndex = Math.Min(Math.Max(delivery.AttemptCount - 1, 0), RetryDelays.Length - 1);
        var nextRetry = utcNow.Add(RetryDelays[delayIndex]);
        delivery.MarkFailed(statusCode, error, nextRetry, MaxAttempts);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
