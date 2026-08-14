using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Contracts.Orders;

namespace Commerce.Orders.Application.Orders;

public sealed class OrderNotificationReader(IOrderRepository orderRepository) : IOrderNotificationReader
{
    public async Task<OrderNotificationContext?> GetAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return null;
        }

        return new OrderNotificationContext(
            order.Id,
            order.OrderNumber,
            order.StoreId,
            order.CustomerId,
            order.CustomerEmail,
            order.GrandTotal,
            order.CurrencyCode);
    }
}
