using Commerce.Framework.Scheduling;
using Commerce.Integration.Contracts.Webhooks;
using Microsoft.Extensions.Logging;

namespace Commerce.Integration.Application.Jobs;

public sealed class WebhookDeliveryJobHandler(
    IWebhookDeliveryProcessor processor,
    ILogger<WebhookDeliveryJobHandler> logger) : IBackgroundJobHandler
{
    public string JobType => BackgroundJobTypes.WebhookDeliveryProcess;

    public async Task<BackgroundJobHandleResult> ExecuteAsync(
        BackgroundJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Processing pending webhook deliveries.");
        await processor.ProcessPendingDeliveriesAsync(cancellationToken).ConfigureAwait(false);
        return new BackgroundJobHandleResult(true);
    }
}
