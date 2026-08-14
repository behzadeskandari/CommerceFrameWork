namespace Commerce.Orders.Contracts.Orders;

public interface IOrderPaidHandler
{
    Task HandleOrderPaidAsync(int orderId, CancellationToken cancellationToken = default);
}
