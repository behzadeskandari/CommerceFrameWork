using Commerce.Inventory.Domain.Entities;
using Commerce.Inventory.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Unit.Inventory;

public sealed class InventoryDomainTests
{
    private static InventoryItem CreateTrackedItem(int onHand = 10, bool allowBackorder = false)
    {
        var item = InventoryItem.Create(1, 100, 10, null, trackInventory: true, allowBackorder);
        if (onHand > 0)
        {
            item.AdjustOnHand(onHand, InventoryMovementType.InitialStock, "seed", InventoryReferenceType.None, null, "test");
        }

        return item;
    }

    [Fact]
    public void Create_SetsOfferIdentityAndPolicies()
    {
        var item = InventoryItem.Create(1, 42, 10, 5, trackInventory: true, allowBackorder: true, warehouseId: 9);

        Assert.Equal(1, item.StoreId);
        Assert.Equal(42, item.OfferId);
        Assert.Equal(10, item.ProductId);
        Assert.Equal(5, item.VariantId);
        Assert.Equal(9, item.WarehouseId);
        Assert.True(item.TrackInventory);
        Assert.True(item.AllowBackorder);
        Assert.Equal(0, item.OnHand);
        Assert.Equal(0, item.Reserved);
    }

    [Fact]
    public void AdjustOnHand_PositiveAndNegative_UpdatesBalanceAndCreatesMovement()
    {
        var item = CreateTrackedItem(0);

        item.AdjustOnHand(10, InventoryMovementType.InitialStock, "initial", InventoryReferenceType.None, null, "admin");
        item.AdjustOnHand(-2, InventoryMovementType.Damage, "damaged", InventoryReferenceType.Manual, null, "admin");

        Assert.Equal(8, item.OnHand);
        Assert.Equal(2, item.Movements.Count);
    }

    [Fact]
    public void AdjustOnHand_RejectsNegativeOnHand()
    {
        var item = CreateTrackedItem(3);
        Assert.Throws<InvalidOperationException>(() =>
            item.AdjustOnHand(-4, InventoryMovementType.Correction, "too much", InventoryReferenceType.Manual, null, "admin"));
    }

    [Fact]
    public void Reserve_DecreasesAvailableAndCreatesReservation()
    {
        var item = CreateTrackedItem(5);
        var utcNow = DateTime.UtcNow;
        var reservation = item.Reserve(2, InventoryReferenceType.Order, 99, utcNow.AddHours(1), utcNow);

        Assert.Equal(2, reservation.Quantity);
        Assert.Equal(2, item.Reserved);
        Assert.Equal(3, item.GetAvailableAt(utcNow));
    }

    [Fact]
    public void Reserve_RejectsWhenInsufficientAndBackorderDisabled()
    {
        var item = CreateTrackedItem(1);
        var utcNow = DateTime.UtcNow;

        Assert.Throws<InvalidOperationException>(() =>
            item.Reserve(2, InventoryReferenceType.Order, 1, utcNow.AddHours(1), utcNow));
    }

    [Fact]
    public void Reserve_AllowsBackorderWhenPolicyEnabled()
    {
        var item = InventoryItem.Create(1, 100, 10, null, trackInventory: true, allowBackorder: true);
        item.AdjustOnHand(1, InventoryMovementType.InitialStock, "seed", InventoryReferenceType.None, null, "test");
        var utcNow = DateTime.UtcNow;

        var reservation = item.Reserve(3, InventoryReferenceType.Order, 7, utcNow.AddHours(1), utcNow);

        Assert.Equal(3, reservation.Quantity);
        Assert.Equal(3, item.Reserved);
        Assert.Equal(InventoryAvailabilityStatus.Backorder, item.GetAvailabilityStatusAt(utcNow));
    }

    [Fact]
    public void ReleaseReservation_IsIdempotent()
    {
        var item = CreateTrackedItem(5);
        var utcNow = DateTime.UtcNow;
        var reservation = item.Reserve(2, InventoryReferenceType.Order, 1, utcNow.AddHours(1), utcNow);

        item.ReleaseReservation(reservation, "cancel", utcNow);
        item.ReleaseReservation(reservation, "cancel again", utcNow);

        Assert.Equal(0, item.Reserved);
        Assert.Equal(InventoryReservationStatus.Released, reservation.Status);
    }

    [Fact]
    public void ExpiredReservation_DoesNotReduceAvailable()
    {
        var item = CreateTrackedItem(5);
        var utcNow = DateTime.UtcNow;
        var reservation = item.Reserve(3, InventoryReferenceType.Order, 1, utcNow.AddMinutes(-1), utcNow.AddMinutes(-2));

        Assert.False(reservation.IsActive(utcNow));
        Assert.Equal(5, item.GetAvailableAt(utcNow));
    }

    [Fact]
    public void NonTrackedItem_DoesNotRequireReservation()
    {
        var item = InventoryItem.Create(1, 100, 10, null, trackInventory: false, allowBackorder: false);
        var utcNow = DateTime.UtcNow;

        Assert.True(item.CanReserveAt(999, utcNow));
        Assert.Equal(InventoryAvailabilityStatus.NotTracked, item.GetAvailabilityStatusAt(utcNow));
        Assert.Throws<InvalidOperationException>(() =>
            item.Reserve(1, InventoryReferenceType.Order, 1, utcNow.AddHours(1), utcNow));
    }

    [Fact]
    public void ConvertReservation_RequiresActiveStatus()
    {
        var item = CreateTrackedItem(5);
        var utcNow = DateTime.UtcNow;
        var reservation = item.Reserve(1, InventoryReferenceType.Order, 1, utcNow.AddHours(1), utcNow);

        reservation.Convert(utcNow);
        Assert.Equal(InventoryReservationStatus.Converted, reservation.Status);

        Assert.Throws<InvalidOperationException>(() => reservation.Release("late", utcNow));
    }
}
