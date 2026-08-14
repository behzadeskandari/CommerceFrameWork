using Commerce.Customers.Contracts.Customers;
using Commerce.Downloads.Contracts.Downloads;
using Commerce.Notifications.Contracts.Dispatch;
using Commerce.Notifications.Domain.Enums;
using Commerce.Orders.Contracts.Orders;

namespace Commerce.Notifications.Application.Handlers;

public sealed class CustomerRegisteredNotificationHandler(
    INotificationEventPublisher publisher,
    ICustomerReader customerReader) : ICustomerRegisteredHandler
{
    public async Task HandleCustomerRegisteredAsync(int customerId, string email, CancellationToken cancellationToken = default)
    {
        var customer = await customerReader.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        await publisher.PublishAsync(
            new NotificationEventRequest(
                NotificationEventType.CustomerRegistered,
                StoreId: null,
                customerId,
                LanguageId: null,
                RecipientEmail: email,
                RecipientPhone: customer.IsSuccess ? customer.Value?.PhoneNumber : null,
                new Dictionary<string, string>
                {
                    ["customerId"] = customerId.ToString(),
                    ["email"] = email,
                    ["firstName"] = customer.IsSuccess ? customer.Value?.FirstName ?? string.Empty : string.Empty
                }),
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed class OrderCreatedNotificationHandler(
    INotificationEventPublisher publisher,
    IOrderNotificationReader orderReader,
    ICustomerReader customerReader) : IOrderCreatedHandler
{
    public async Task HandleOrderCreatedAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await PublishOrderEventAsync(NotificationEventType.OrderCreated, orderId, cancellationToken).ConfigureAwait(false);
    }

    internal async Task PublishOrderEventAsync(
        NotificationEventType eventType,
        int orderId,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? extra = null)
    {
        var detail = await orderReader.GetAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (detail is null)
        {
            return;
        }

        string? email = detail.CustomerEmail;
        string? phone = null;

        if (detail.CustomerId.HasValue)
        {
            var customer = await customerReader.GetByIdAsync(detail.CustomerId.Value, cancellationToken).ConfigureAwait(false);
            email ??= customer.Value?.Email;
            phone = customer.Value?.PhoneNumber;
        }

        var variables = new Dictionary<string, string>
        {
            ["orderId"] = detail.Id.ToString(),
            ["orderNumber"] = detail.OrderNumber,
            ["grandTotal"] = detail.GrandTotal.ToString("F2"),
            ["currencyCode"] = detail.CurrencyCode
        };

        if (extra is not null)
        {
            foreach (var pair in extra)
            {
                variables[pair.Key] = pair.Value;
            }
        }

        await publisher.PublishAsync(
            new NotificationEventRequest(
                eventType,
                detail.StoreId,
                detail.CustomerId,
                LanguageId: null,
                email,
                phone,
                variables),
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed class OrderPaidNotificationHandler(OrderCreatedNotificationHandler orderHandler) : IOrderPaidHandler
{
    public Task HandleOrderPaidAsync(int orderId, CancellationToken cancellationToken = default) =>
        orderHandler.PublishOrderEventAsync(NotificationEventType.PaymentSucceeded, orderId, cancellationToken);
}

public sealed class OrderPaymentFailedNotificationHandler(
    OrderCreatedNotificationHandler orderHandler) : IOrderPaymentFailedHandler
{
    public Task HandleOrderPaymentFailedAsync(int orderId, string? reason, CancellationToken cancellationToken = default) =>
        orderHandler.PublishOrderEventAsync(
            NotificationEventType.PaymentFailed,
            orderId,
            cancellationToken,
            new Dictionary<string, string> { ["reason"] = reason ?? string.Empty });
}

public sealed class OrderCancelledNotificationHandler(
    OrderCreatedNotificationHandler orderHandler) : IOrderCancelledHandler
{
    public Task HandleOrderCancelledAsync(int orderId, string reason, CancellationToken cancellationToken = default) =>
        orderHandler.PublishOrderEventAsync(
            NotificationEventType.OrderCancelled,
            orderId,
            cancellationToken,
            new Dictionary<string, string> { ["reason"] = reason });
}

public sealed class OrderRefundNotificationHandler(
    OrderCreatedNotificationHandler orderHandler) : IOrderRefundHandler
{
    public Task HandleOrderRefundAsync(int orderId, bool isFullRefund, string? reason, CancellationToken cancellationToken = default) =>
        orderHandler.PublishOrderEventAsync(
            NotificationEventType.RefundCreated,
            orderId,
            cancellationToken,
            new Dictionary<string, string>
            {
                ["refundType"] = isFullRefund ? "full" : "partial",
                ["reason"] = reason ?? string.Empty
            });
}

public sealed class ShipmentCreatedNotificationHandler(
    OrderCreatedNotificationHandler orderHandler) : IShipmentCreatedHandler
{
    public Task HandleShipmentCreatedAsync(int orderId, string? trackingNumber, CancellationToken cancellationToken = default) =>
        orderHandler.PublishOrderEventAsync(
            NotificationEventType.ShipmentCreated,
            orderId,
            cancellationToken,
            new Dictionary<string, string> { ["trackingNumber"] = trackingNumber ?? string.Empty });
}

public sealed class OrderReturnNotificationHandler(
    OrderCreatedNotificationHandler orderHandler) : IOrderReturnHandler
{
    public Task HandleReturnRequestedAsync(int returnCaseId, int orderId, CancellationToken cancellationToken = default) =>
        PublishAsync(NotificationEventType.ReturnRequested, returnCaseId, orderId, cancellationToken);

    public Task HandleReturnApprovedAsync(int returnCaseId, int orderId, CancellationToken cancellationToken = default) =>
        PublishAsync(NotificationEventType.ReturnApproved, returnCaseId, orderId, cancellationToken);

    public Task HandleReturnRejectedAsync(int returnCaseId, int orderId, string reason, CancellationToken cancellationToken = default) =>
        PublishAsync(
            NotificationEventType.ReturnRejected,
            returnCaseId,
            orderId,
            cancellationToken,
            new Dictionary<string, string> { ["reason"] = reason });

    public Task HandleReturnCompletedAsync(int returnCaseId, int orderId, CancellationToken cancellationToken = default) =>
        PublishAsync(NotificationEventType.ReturnCompleted, returnCaseId, orderId, cancellationToken);

    private Task PublishAsync(
        NotificationEventType eventType,
        int returnCaseId,
        int orderId,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? extra = null)
    {
        var metadata = new Dictionary<string, string> { ["returnCaseId"] = returnCaseId.ToString() };
        if (extra is not null)
        {
            foreach (var pair in extra)
            {
                metadata[pair.Key] = pair.Value;
            }
        }

        return orderHandler.PublishOrderEventAsync(eventType, orderId, cancellationToken, metadata);
    }
}

public sealed class DownloadAvailableNotificationHandler(
    INotificationEventPublisher publisher,
    ICustomerReader customerReader) : IDownloadAvailableHandler
{
    public async Task HandleDownloadAvailableAsync(
        int customerId,
        int orderId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerReader.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (!customer.IsSuccess || customer.Value is null)
        {
            return;
        }

        await publisher.PublishAsync(
            new NotificationEventRequest(
                NotificationEventType.DownloadAvailable,
                StoreId: null,
                customerId,
                LanguageId: null,
                customer.Value.Email,
                customer.Value.PhoneNumber,
                new Dictionary<string, string>
                {
                    ["customerId"] = customerId.ToString(),
                    ["orderId"] = orderId.ToString(),
                    ["productId"] = productId.ToString()
                }),
            cancellationToken).ConfigureAwait(false);
    }
}
