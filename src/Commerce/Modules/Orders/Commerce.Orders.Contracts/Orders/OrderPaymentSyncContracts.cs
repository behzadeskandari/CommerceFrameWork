using Commerce.Orders.Domain.Entities;



namespace Commerce.Orders.Contracts.Orders;



public interface IOrderPaymentSyncRepository

{

    Task<Order?> GetByIdAsync(int orderId, CancellationToken cancellationToken = default);



    Task SaveAsync(Order order, CancellationToken cancellationToken = default);

}

