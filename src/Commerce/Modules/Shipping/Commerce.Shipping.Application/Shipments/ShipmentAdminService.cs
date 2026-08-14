using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Orders.Contracts.Orders;
using Commerce.Shipping.Application.Abstractions;
using Commerce.Shipping.Contracts.Shipments;
using Commerce.Shipping.Domain.Entities;
using Commerce.Shipping.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Shipping.Application.Shipments;

public sealed class ShipmentAdminService(
    IShippingRepository repository,
    IServiceScopeFactory scopeFactory,
    IOrderFulfillmentSync orderFulfillmentSync,
    IEnumerable<IShipmentCreatedHandler> shipmentCreatedHandlers) : IShipmentAdminService
{
    public async Task<Result<IReadOnlyList<ShipmentSummaryDto>>> ListByOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var shipments = await repository.ListShipmentsByOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<ShipmentSummaryDto>>(shipments.Select(MapSummary).ToList());
    }

    public async Task<Result<ShipmentDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var shipment = await repository.GetShipmentByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return shipment is null
            ? Result.Failure<ShipmentDetailDto>(Error.NotFound($"Shipment '{id}' was not found."))
            : Result.Success(MapDetail(shipment));
    }

    public async Task<Result<ShipmentDetailDto>> CreateAsync(
        CreateShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var orderResult = await orderService.GetByIdAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (orderResult.IsFailure)
        {
            return Result.Failure<ShipmentDetailDto>(orderResult.Error!);
        }

        var order = orderResult.Value!;
        if (!order.RequiresShipping)
        {
            return Result.Failure<ShipmentDetailDto>(Error.Validation("Shipments cannot be created for digital-only orders."));
        }

        foreach (var line in request.Items)
        {
            var orderLine = order.Items.FirstOrDefault(x => x.Id == line.OrderItemId);
            if (orderLine is null)
            {
                return Result.Failure<ShipmentDetailDto>(Error.Validation($"Order item '{line.OrderItemId}' was not found."));
            }

            var alreadyShipped = await repository
                .GetShippedQuantityForOrderItemAsync(line.OrderItemId, cancellationToken)
                .ConfigureAwait(false);

            if (alreadyShipped + line.Quantity > orderLine.Quantity)
            {
                return Result.Failure<ShipmentDetailDto>(Error.Validation($"Cannot ship more than ordered quantity for item '{line.OrderItemId}'."));
            }
        }

        var shipment = Shipment.Create(
            request.OrderId,
            order.StoreId,
            request.ShippingMethodId,
            request.ProviderSystemName,
            request.Notes,
            request.Items.Select(x => ShipmentItem.Create(x.OrderItemId, x.OfferId, x.ProductId, x.Quantity)));

        await repository.AddShipmentAsync(shipment, cancellationToken).ConfigureAwait(false);
        await orderFulfillmentSync.SyncFulfillmentAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(shipment));
    }

    public async Task<Result<ShipmentDetailDto>> UpdateTrackingAsync(
        int id,
        UpdateShipmentTrackingRequest request,
        CancellationToken cancellationToken = default)
    {
        var shipment = await repository.GetShipmentByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (shipment is null)
        {
            return Result.Failure<ShipmentDetailDto>(Error.NotFound($"Shipment '{id}' was not found."));
        }

        shipment.SetTracking(request.TrackingNumber, request.TrackingUrl, request.CarrierName);
        await repository.SaveShipmentAsync(shipment, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(shipment));
    }

    public async Task<Result<ShipmentDetailDto>> MarkShippedAsync(int id, CancellationToken cancellationToken = default)
    {
        var shipment = await repository.GetShipmentByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (shipment is null)
        {
            return Result.Failure<ShipmentDetailDto>(Error.NotFound($"Shipment '{id}' was not found."));
        }

        var utcNow = DateTime.UtcNow;
        shipment.MarkShipped(utcNow);
        await repository.SaveShipmentAsync(shipment, cancellationToken).ConfigureAwait(false);
        await orderFulfillmentSync.SyncFulfillmentAsync(shipment.OrderId, cancellationToken).ConfigureAwait(false);

        foreach (var handler in shipmentCreatedHandlers)
        {
            await handler.HandleShipmentCreatedAsync(shipment.OrderId, shipment.TrackingNumber, cancellationToken)
                .ConfigureAwait(false);
        }

        return Result.Success(MapDetail(shipment));
    }

    public async Task<Result<ShipmentDetailDto>> MarkDeliveredAsync(int id, CancellationToken cancellationToken = default)
    {
        var shipment = await repository.GetShipmentByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (shipment is null)
        {
            return Result.Failure<ShipmentDetailDto>(Error.NotFound($"Shipment '{id}' was not found."));
        }

        shipment.MarkDelivered(DateTime.UtcNow);
        await repository.SaveShipmentAsync(shipment, cancellationToken).ConfigureAwait(false);
        await orderFulfillmentSync.SyncFulfillmentAsync(shipment.OrderId, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(shipment));
    }

    public async Task<Result> CancelOpenShipmentsForOrderAsync(
        int orderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var shipments = await repository.ListShipmentsByOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        foreach (var shipment in shipments.Where(x => x.Status is ShipmentStatus.Pending or ShipmentStatus.Shipped))
        {
            try
            {
                shipment.Cancel(reason);
                await repository.SaveShipmentAsync(shipment, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                // Shipment may already be terminal; skip.
            }
        }

        await orderFulfillmentSync.SyncFulfillmentAsync(orderId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static ShipmentSummaryDto MapSummary(Shipment shipment) =>
        new(
            shipment.Id,
            shipment.OrderId,
            shipment.StoreId,
            shipment.Status,
            shipment.TrackingNumber,
            shipment.CarrierName,
            shipment.ShippedAtUtc,
            shipment.CreatedAtUtc);

    private static ShipmentDetailDto MapDetail(Shipment shipment) =>
        new(
            shipment.Id,
            shipment.OrderId,
            shipment.StoreId,
            shipment.ShippingMethodId,
            shipment.ProviderSystemName,
            shipment.Status,
            shipment.TrackingNumber,
            shipment.TrackingUrl,
            shipment.CarrierName,
            shipment.Notes,
            shipment.ShippedAtUtc,
            shipment.DeliveredAtUtc,
            shipment.Items.Select(x => new ShipmentItemDto(x.Id, x.OrderItemId, x.OfferId, x.ProductId, x.Quantity)).ToList(),
            shipment.CreatedAtUtc,
            shipment.UpdatedAtUtc);
}
