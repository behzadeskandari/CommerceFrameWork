using Commerce.Downloads.Application.Abstractions;

namespace Commerce.Downloads.Application.Entitlements;

public sealed class DownloadEntitlementGrantHandler(
    IDownloadEntitlementService entitlementService) : Commerce.Orders.Contracts.Orders.IOrderPaidHandler
{
    public Task HandleOrderPaidAsync(int orderId, CancellationToken cancellationToken = default) =>
        entitlementService.GrantForPaidOrderAsync(orderId, cancellationToken);
}
