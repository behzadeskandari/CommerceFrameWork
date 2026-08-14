using Commerce.Inventory.Application.Abstractions;
using Commerce.Inventory.Contracts.Inventory;
using Commerce.Orders.Contracts.Orders;
using Microsoft.Extensions.Logging;

namespace Commerce.Inventory.Application.Integration;

public sealed class OrderPaidInventoryHandler(
    IInventoryOrderService inventoryOrderService,
    IInventoryRepository inventoryRepository,
    ILogger<OrderPaidInventoryHandler> logger) : IOrderPaidHandler
{
    public async Task HandleOrderPaidAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var reservations = await inventoryRepository
            .GetActiveReservationsForReferenceAsync(Domain.Enums.InventoryReferenceType.Order, orderId, cancellationToken)
            .ConfigureAwait(false);

        if (reservations.Count == 0)
        {
            return;
        }

        var item = await inventoryRepository
            .GetByIdWithDetailsAsync(reservations[0].InventoryItemId, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            return;
        }

        var result = await inventoryOrderService
            .ConvertForOrderAsync(orderId, item.StoreId, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Inventory conversion failed for paid order {OrderId}: {Error}",
                orderId,
                result.Error?.Message);
        }
    }
}
