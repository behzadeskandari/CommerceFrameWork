using Commerce.Orders.Contracts.Orders;
using Commerce.Payments.Contracts.Payments;
using Microsoft.Extensions.Logging;

namespace Commerce.Payments.Application.Payments;

public sealed class OrderPaymentSyncService(
    IOrderPaymentSyncRepository orderRepository,
    IEnumerable<IOrderPaidHandler> orderPaidHandlers,
    IEnumerable<IOrderPaymentFailedHandler> orderPaymentFailedHandlers,
    IEnumerable<IOrderRefundHandler> orderRefundHandlers,
    ILogger<OrderPaymentSyncService> logger) : IOrderPaymentSyncService
{
    public async Task SyncAuthorizedAsync(int orderId, string? reason = null, CancellationToken cancellationToken = default)
    {
        await UpdateOrderAsync(orderId, order => order.ApplyPaymentAuthorized(reason), cancellationToken).ConfigureAwait(false);
    }

    public async Task SyncPaidAsync(int orderId, string? reason = null, CancellationToken cancellationToken = default)
    {
        await UpdateOrderAsync(orderId, order => order.MarkPaymentPaid(reason), cancellationToken).ConfigureAwait(false);
        await NotifyOrderPaidHandlersAsync(orderId, cancellationToken).ConfigureAwait(false);
    }

    public async Task SyncFailedAsync(int orderId, string? reason = null, CancellationToken cancellationToken = default)
    {
        await UpdateOrderAsync(orderId, order => order.MarkPaymentFailed(reason), cancellationToken).ConfigureAwait(false);
        await NotifyHandlersAsync(orderPaymentFailedHandlers, handler => handler.HandleOrderPaymentFailedAsync(orderId, reason, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    public async Task SyncPartialRefundAsync(int orderId, string? reason = null, CancellationToken cancellationToken = default)
    {
        await UpdateOrderAsync(orderId, order => order.ApplyPartialRefund(reason), cancellationToken).ConfigureAwait(false);
        await NotifyHandlersAsync(orderRefundHandlers, handler => handler.HandleOrderRefundAsync(orderId, isFullRefund: false, reason, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    public async Task SyncFullRefundAsync(int orderId, string? reason = null, CancellationToken cancellationToken = default)
    {
        await UpdateOrderAsync(orderId, order => order.ApplyFullRefund(reason), cancellationToken).ConfigureAwait(false);
        await NotifyHandlersAsync(orderRefundHandlers, handler => handler.HandleOrderRefundAsync(orderId, isFullRefund: true, reason, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    private async Task NotifyOrderPaidHandlersAsync(int orderId, CancellationToken cancellationToken)
    {
        foreach (var handler in orderPaidHandlers)
        {
            await handler.HandleOrderPaidAsync(orderId, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task NotifyHandlersAsync<THandler>(
        IEnumerable<THandler> handlers,
        Func<THandler, Task> invoke,
        CancellationToken cancellationToken)
    {
        foreach (var handler in handlers)
        {
            await invoke(handler).ConfigureAwait(false);
        }
    }

    private async Task UpdateOrderAsync(
        int orderId,
        Action<Commerce.Orders.Domain.Entities.Order> update,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found for payment sync.", orderId);
            return;
        }

        update(order);
        await orderRepository.SaveAsync(order, cancellationToken).ConfigureAwait(false);
    }
}
