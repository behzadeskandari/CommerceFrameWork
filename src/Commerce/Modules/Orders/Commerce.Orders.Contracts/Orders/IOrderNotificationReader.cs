namespace Commerce.Orders.Contracts.Orders;

public sealed record OrderNotificationContext(
    int Id,
    string OrderNumber,
    int StoreId,
    int? CustomerId,
    string? CustomerEmail,
    decimal GrandTotal,
    string CurrencyCode);

public interface IOrderNotificationReader
{
    Task<OrderNotificationContext?> GetAsync(int orderId, CancellationToken cancellationToken = default);
}
