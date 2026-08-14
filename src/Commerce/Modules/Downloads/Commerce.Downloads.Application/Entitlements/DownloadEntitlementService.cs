using Commerce.Catalog.Contracts.Products;
using Commerce.Downloads.Application.Abstractions;
using Commerce.Downloads.Contracts.Downloads;
using Commerce.Downloads.Domain.Entities;
using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Commerce.Downloads.Application.Entitlements;

public sealed class DownloadEntitlementService(
    IOrderPaymentSyncRepository orderRepository,
    IProductReader productReader,
    IDownloadRepository downloadRepository,
    IEnumerable<IDownloadAvailableHandler> downloadAvailableHandlers,
    ILogger<DownloadEntitlementService> logger) : IDownloadEntitlementService
{
    public async Task GrantForPaidOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found for download entitlement grant.", orderId);
            return;
        }

        if (order.PaymentStatus != PaymentStatus.Paid)
        {
            return;
        }

        var grantedAt = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            if (await downloadRepository.EntitlementExistsForOrderItemAsync(item.Id, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var product = await productReader.GetByIdAsync(item.ProductId, cancellationToken).ConfigureAwait(false);
            if (!product.IsSuccess || product.Value is null || !DigitalProductTypes.IsDigital(product.Value.ProductType))
            {
                continue;
            }

            var settings = await downloadRepository
                .GetSettingsAsync(item.ProductId, order.StoreId, cancellationToken)
                .ConfigureAwait(false);

            if (settings is null || !settings.IsEnabled)
            {
                continue;
            }

            var files = await downloadRepository
                .ListFilesAsync(item.ProductId, order.StoreId, cancellationToken)
                .ConfigureAwait(false);

            if (files.Count == 0 || files.All(x => !x.IsActive))
            {
                continue;
            }

            var entitlement = DownloadEntitlement.Grant(
                order.Id,
                item.Id,
                item.ProductId,
                order.StoreId,
                order.CustomerId,
                order.GuestAccessToken,
                grantedAt,
                settings.CalculateExpirationUtc(grantedAt),
                settings.MaxDownloadCount);

            await downloadRepository.AddEntitlementAsync(entitlement, cancellationToken).ConfigureAwait(false);
            logger.LogInformation(
                "Granted download entitlement for order {OrderId}, product {ProductId}.",
                order.Id,
                item.ProductId);

            if (order.CustomerId.HasValue)
            {
                foreach (var handler in downloadAvailableHandlers)
                {
                    await handler.HandleDownloadAvailableAsync(
                        order.CustomerId.Value,
                        order.Id,
                        item.ProductId,
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        await downloadRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
