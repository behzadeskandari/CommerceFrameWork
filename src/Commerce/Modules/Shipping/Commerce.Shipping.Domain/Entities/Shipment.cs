using Commerce.Framework.Core.Entities;
using Commerce.Shipping.Domain.Enums;

namespace Commerce.Shipping.Domain.Entities;

public sealed class Shipment : AggregateRoot
{
    public const int TrackingNumberMaxLength = 128;
    public const int TrackingUrlMaxLength = 512;
    public const int CarrierNameMaxLength = 128;
    public const int ProviderSystemNameMaxLength = 128;
    public const int NotesMaxLength = 2000;

    private readonly List<ShipmentItem> _items = [];

    private Shipment()
    {
    }

    public int OrderId { get; private set; }

    public int StoreId { get; private set; }

    public int? ShippingMethodId { get; private set; }

    public string? ProviderSystemName { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public string? TrackingNumber { get; private set; }

    public string? TrackingUrl { get; private set; }

    public string? CarrierName { get; private set; }

    public string? Notes { get; private set; }

    public DateTime? ShippedAtUtc { get; private set; }

    public DateTime? DeliveredAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ShipmentItem> Items => _items;

    public static Shipment Create(
        int orderId,
        int storeId,
        int? shippingMethodId,
        string? providerSystemName,
        string? notes,
        IEnumerable<ShipmentItem> items)
    {
        if (orderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderId));
        }

        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            throw new InvalidOperationException("Shipment must contain at least one item.");
        }

        var utcNow = DateTime.UtcNow;
        var shipment = new Shipment
        {
            OrderId = orderId,
            StoreId = storeId,
            ShippingMethodId = shippingMethodId,
            ProviderSystemName = string.IsNullOrWhiteSpace(providerSystemName) ? null : providerSystemName.Trim(),
            Status = ShipmentStatus.Pending,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        shipment._items.AddRange(itemList);
        return shipment;
    }

    public void SetTracking(string? trackingNumber, string? trackingUrl, string? carrierName)
    {
        TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim();
        TrackingUrl = string.IsNullOrWhiteSpace(trackingUrl) ? null : trackingUrl.Trim();
        CarrierName = string.IsNullOrWhiteSpace(carrierName) ? null : carrierName.Trim();
        Touch();
    }

    public void MarkShipped(DateTime utcNow)
    {
        if (Status is ShipmentStatus.Cancelled or ShipmentStatus.Delivered)
        {
            throw new InvalidOperationException($"Shipment cannot be shipped from status {Status}.");
        }

        Status = ShipmentStatus.Shipped;
        ShippedAtUtc ??= utcNow;
        Touch();
    }

    public void MarkDelivered(DateTime utcNow)
    {
        if (Status is ShipmentStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled shipment cannot be delivered.");
        }

        Status = ShipmentStatus.Delivered;
        ShippedAtUtc ??= utcNow;
        DeliveredAtUtc = utcNow;
        Touch();
    }

    public void Cancel(string reason)
    {
        if (Status is ShipmentStatus.Delivered)
        {
            throw new InvalidOperationException("Delivered shipment cannot be cancelled.");
        }

        Status = ShipmentStatus.Cancelled;
        Notes = string.IsNullOrWhiteSpace(reason) ? Notes : reason.Trim();
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}

public sealed class ShipmentItem : Entity
{
    private ShipmentItem()
    {
    }

    public int ShipmentId { get; private set; }

    public int OrderItemId { get; private set; }

    public int OfferId { get; private set; }

    public int ProductId { get; private set; }

    public int Quantity { get; private set; }

    public static ShipmentItem Create(int orderItemId, int offerId, int productId, int quantity)
    {
        if (orderItemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderItemId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        return new ShipmentItem
        {
            OrderItemId = orderItemId,
            OfferId = offerId,
            ProductId = productId,
            Quantity = quantity
        };
    }
}
