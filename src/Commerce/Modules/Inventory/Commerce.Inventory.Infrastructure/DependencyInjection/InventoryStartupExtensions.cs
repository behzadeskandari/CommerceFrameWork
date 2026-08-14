using Commerce.Framework.Scheduling;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Inventory.Infrastructure.DependencyInjection;

public static class InventoryStartupExtensions
{
    public static async Task RegisterInventoryRecurringJobsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();

        await scheduler.RegisterRecurringAsync(
            new RegisterRecurringJobRequest(
                "inventory.reservations.expire",
                BackgroundJobTypes.InventoryReservationExpire,
                300),
            cancellationToken).ConfigureAwait(false);
    }
}
