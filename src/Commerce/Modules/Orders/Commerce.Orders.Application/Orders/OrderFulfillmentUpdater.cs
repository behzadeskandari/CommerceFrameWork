using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Domain.Enums;

namespace Commerce.Orders.Application.Orders;

public sealed class OrderFulfillmentUpdater(IOrderRepository orderRepository) : IOrderFulfillmentUpdater
{
    public async Task<Result> UpdateFulfillmentStatusAsync(
        int orderId,
        FulfillmentStatus status,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure(Error.NotFound($"Order '{orderId}' was not found."));
        }

        order.UpdateFulfillmentStatus(status, reason, "shipping");
        await orderRepository.SaveAsync(order, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
