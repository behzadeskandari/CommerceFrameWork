namespace Commerce.Orders.Contracts.Orders;

public interface IOrderPurchaseVerifier
{
    Task<bool> HasCustomerPurchasedProductAsync(
        int customerId,
        int productId,
        int storeId,
        CancellationToken cancellationToken = default);
}
