using Commerce.Framework.Core.Results;
using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Domain.Enums;
using Commerce.Shipping.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Shipping.Application.Shipments;

public sealed class OrderFulfillmentSync(
    IServiceScopeFactory scopeFactory,
    IShippingRepository shippingRepository,
    IOrderFulfillmentUpdater fulfillmentUpdater) : IOrderFulfillmentSync
{
    public async Task<Result> SyncFulfillmentAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var orderResult = await orderService.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (orderResult.IsFailure)
        {
            return Result.Failure(orderResult.Error!);
        }

        var order = orderResult.Value!;
        if (!order.RequiresShipping)
        {
            return Result.Success();
        }

        var shippableItems = order.Items;
        if (shippableItems.Count == 0)
        {
            return Result.Success();
        }

        var fullyShipped = true;
        var anyShipped = false;

        foreach (var item in shippableItems)
        {
            var shipped = await shippingRepository
                .GetShippedQuantityForOrderItemAsync(item.Id, cancellationToken)
                .ConfigureAwait(false);

            if (shipped > 0)
            {
                anyShipped = true;
            }

            if (shipped < item.Quantity)
            {
                fullyShipped = false;
            }
        }

        var targetStatus = fullyShipped
            ? FulfillmentStatus.Fulfilled
            : anyShipped
                ? FulfillmentStatus.PartiallyFulfilled
                : FulfillmentStatus.Unfulfilled;

        if (order.FulfillmentStatus == targetStatus)
        {
            return Result.Success();
        }

        return await fulfillmentUpdater
            .UpdateFulfillmentStatusAsync(orderId, targetStatus, "Shipment sync.", cancellationToken)
            .ConfigureAwait(false);
    }
}
