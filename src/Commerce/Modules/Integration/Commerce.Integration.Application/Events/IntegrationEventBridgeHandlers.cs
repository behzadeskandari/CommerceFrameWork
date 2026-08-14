using Commerce.Catalog.Domain.Events;
using Commerce.Customers.Contracts.Customers;
using Commerce.Customers.Domain.Events;
using Commerce.Framework.Core.Events;
using Commerce.Framework.Events;
using Commerce.Integration.Contracts.Events;
using Commerce.Inventory.Domain.Events;
using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Domain.Events;

namespace Commerce.Integration.Application.Events;

public sealed class CatalogInventoryDomainEventMapper : IDomainEventIntegrationMapper
{
    public IEnumerable<IIntegrationEvent> Map(IDomainEvent domainEvent) =>
        domainEvent switch
        {
            ProductCreatedEvent created => new[]
            {
                new ProductCreatedIntegrationEvent(created.ProductId, created.Sku, created.Name)
                {
                    StoreId = null
                }
            },
            ProductUpdatedEvent updated => new[]
            {
                new ProductUpdatedIntegrationEvent(updated.ProductId, updated.Sku, updated.Name)
                {
                    StoreId = null
                }
            },
            InventoryAdjustedEvent adjusted => new[]
            {
                new InventoryChangedIntegrationEvent(
                    adjusted.InventoryItemId,
                    adjusted.OfferId,
                    adjusted.QuantityDelta,
                    adjusted.MovementType,
                    0)
                {
                    StoreId = adjusted.StoreId
                }
            },
            _ => []
        };
}

public sealed class IntegrationOrderCreatedHandler(
    IIntegrationEventPublisher publisher,
    IOrderNotificationReader orderReader) : IOrderCreatedHandler
{
    public async Task HandleOrderCreatedAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderReader.GetAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return;
        }

        await publisher.PublishAsync(
            new OrderCreatedIntegrationEvent(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                order.GrandTotal,
                order.CurrencyCode)
            {
                StoreId = order.StoreId
            },
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed class IntegrationOrderPaidHandler(
    IIntegrationEventPublisher publisher,
    IOrderNotificationReader orderReader) : IOrderPaidHandler
{
    public async Task HandleOrderPaidAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderReader.GetAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return;
        }

        await publisher.PublishAsync(
            new OrderPaidIntegrationEvent(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                order.GrandTotal,
                order.CurrencyCode)
            {
                StoreId = order.StoreId
            },
            cancellationToken).ConfigureAwait(false);

        await publisher.PublishAsync(
            new PaymentSucceededIntegrationEvent(
                order.Id,
                null,
                order.GrandTotal,
                order.CurrencyCode,
                null)
            {
                StoreId = order.StoreId
            },
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed class IntegrationOrderCancelledHandler(
    IIntegrationEventPublisher publisher,
    IOrderNotificationReader orderReader) : IOrderCancelledHandler
{
    public async Task HandleOrderCancelledAsync(int orderId, string reason, CancellationToken cancellationToken = default)
    {
        var order = await orderReader.GetAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return;
        }

        await publisher.PublishAsync(
            new OrderCancelledIntegrationEvent(order.Id, order.OrderNumber, reason)
            {
                StoreId = order.StoreId
            },
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed class IntegrationOrderPaymentFailedHandler(
    IIntegrationEventPublisher publisher,
    IOrderNotificationReader orderReader) : IOrderPaymentFailedHandler
{
    public async Task HandleOrderPaymentFailedAsync(int orderId, string? reason, CancellationToken cancellationToken = default)
    {
        var order = await orderReader.GetAsync(orderId, cancellationToken).ConfigureAwait(false);

        await publisher.PublishAsync(
            new PaymentFailedIntegrationEvent(orderId, null, reason)
            {
                StoreId = order?.StoreId
            },
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed class IntegrationOrderRefundHandler(
    IIntegrationEventPublisher publisher,
    IOrderNotificationReader orderReader) : IOrderRefundHandler
{
    public async Task HandleOrderRefundAsync(
        int orderId,
        bool isFullRefund,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var order = await orderReader.GetAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return;
        }

        await publisher.PublishAsync(
            new RefundCreatedIntegrationEvent(order.Id, order.OrderNumber, isFullRefund, reason)
            {
                StoreId = order.StoreId
            },
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed class IntegrationShipmentCreatedHandler(
    IIntegrationEventPublisher publisher,
    IOrderNotificationReader orderReader) : IShipmentCreatedHandler
{
    public async Task HandleShipmentCreatedAsync(
        int orderId,
        string? trackingNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await orderReader.GetAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return;
        }

        await publisher.PublishAsync(
            new ShipmentCreatedIntegrationEvent(order.Id, order.OrderNumber, trackingNumber)
            {
                StoreId = order.StoreId
            },
            cancellationToken).ConfigureAwait(false);
    }
}

public sealed class IntegrationCustomerRegisteredHandler(IIntegrationEventPublisher publisher) : ICustomerRegisteredHandler
{
    public Task HandleCustomerRegisteredAsync(int customerId, string email, CancellationToken cancellationToken = default) =>
        publisher.PublishAsync(
            new CustomerRegisteredIntegrationEvent(customerId, email),
            cancellationToken);
}
