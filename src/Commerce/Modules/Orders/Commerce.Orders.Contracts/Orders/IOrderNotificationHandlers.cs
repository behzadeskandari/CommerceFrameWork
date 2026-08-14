namespace Commerce.Orders.Contracts.Orders;

public interface IOrderCreatedHandler
{
    Task HandleOrderCreatedAsync(int orderId, CancellationToken cancellationToken = default);
}

public interface IOrderCancelledHandler
{
    Task HandleOrderCancelledAsync(int orderId, string reason, CancellationToken cancellationToken = default);
}

public interface IOrderPaymentFailedHandler
{
    Task HandleOrderPaymentFailedAsync(int orderId, string? reason, CancellationToken cancellationToken = default);
}

public interface IOrderRefundHandler
{
    Task HandleOrderRefundAsync(int orderId, bool isFullRefund, string? reason, CancellationToken cancellationToken = default);
}

public interface IOrderReturnHandler
{
    Task HandleReturnRequestedAsync(int returnCaseId, int orderId, CancellationToken cancellationToken = default);

    Task HandleReturnApprovedAsync(int returnCaseId, int orderId, CancellationToken cancellationToken = default);

    Task HandleReturnRejectedAsync(int returnCaseId, int orderId, string reason, CancellationToken cancellationToken = default);

    Task HandleReturnCompletedAsync(int returnCaseId, int orderId, CancellationToken cancellationToken = default);
}

public interface IShipmentCreatedHandler
{
    Task HandleShipmentCreatedAsync(int orderId, string? trackingNumber, CancellationToken cancellationToken = default);
}
