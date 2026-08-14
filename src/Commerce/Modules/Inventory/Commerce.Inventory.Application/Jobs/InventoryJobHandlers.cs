using Commerce.Framework.Scheduling;
using Commerce.Inventory.Contracts.Inventory;
using Microsoft.Extensions.Logging;

namespace Commerce.Inventory.Application.Jobs;

public sealed class InventoryReservationExpirationJobHandler(
    IInventoryReservationExpirationService expirationService,
    ILogger<InventoryReservationExpirationJobHandler> logger) : IBackgroundJobHandler
{
    public string JobType => BackgroundJobTypes.InventoryReservationExpire;

    public async Task<BackgroundJobHandleResult> ExecuteAsync(
        BackgroundJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var count = await expirationService.ExpireReservationsAsync(cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Inventory expiration job released {Count} reservation(s).", count);
        return new BackgroundJobHandleResult(true);
    }
}
