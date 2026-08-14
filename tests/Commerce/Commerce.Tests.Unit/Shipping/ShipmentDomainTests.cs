using Commerce.Shipping.Contracts.Shipping;
using Commerce.Shipping.Domain.Entities;
using Commerce.Shipping.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Unit.Shipping;

public sealed class ShipmentDomainTests
{
    [Fact]
    public void CreateShipment_TracksItemsAndPendingStatus()
    {
        var shipment = Shipment.Create(
            10,
            1,
            5,
            ShippingProviderNames.FlatRate,
            "First shipment",
            [ShipmentItem.Create(100, 20, 30, 2)]);

        Assert.Equal(ShipmentStatus.Pending, shipment.Status);
        Assert.Single(shipment.Items);
        Assert.Equal(2, shipment.Items.First().Quantity);
    }

    [Fact]
    public void MarkShipped_SetsTrackingLifecycle()
    {
        var shipment = Shipment.Create(
            10,
            1,
            5,
            ShippingProviderNames.FlatRate,
            null,
            [ShipmentItem.Create(100, 20, 30, 1)]);

        shipment.SetTracking("TRK-1", "https://track.example/TRK-1", "Courier");
        shipment.MarkShipped(DateTime.UtcNow);

        Assert.Equal(ShipmentStatus.Shipped, shipment.Status);
        Assert.Equal("TRK-1", shipment.TrackingNumber);
        Assert.NotNull(shipment.ShippedAtUtc);
    }
}
