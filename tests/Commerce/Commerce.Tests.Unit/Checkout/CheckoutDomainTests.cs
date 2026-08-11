using Commerce.Checkout.Domain.Entities;
using Commerce.Checkout.Domain.Enums;
using Commerce.Checkout.Domain.ValueObjects;
using Xunit;

namespace Commerce.Tests.Unit.Checkout;

public sealed class CheckoutDomainTests
{
    [Fact]
    public void Create_RejectsEmptyCart()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CheckoutSession.Create(
                1,
                1,
                null,
                "guest-token",
                1,
                "IRR",
                requiresShipping: true,
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                []));
    }

    [Fact]
    public void Create_AssignsActiveStatus()
    {
        var item = CheckoutSessionItem.Create(0, 1, 10, 5, null, 2, 100m, 200m, "IRR", 100m);
        var session = CheckoutSession.Create(
            1,
            1,
            null,
            "guest-token",
            1,
            "IRR",
            requiresShipping: true,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            [item]);

        Assert.Equal(CheckoutStatus.Active, session.Status);
        Assert.Equal(200m, session.Subtotal);
    }

    [Fact]
    public void ReplaceItems_MarksRequiresReviewWhenPriceChanged()
    {
        var item = CheckoutSessionItem.Create(0, 1, 10, 5, null, 1, 100m, 100m, "IRR", 90m);
        var session = CheckoutSession.Create(
            1,
            1,
            null,
            "guest-token",
            1,
            "IRR",
            requiresShipping: false,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            [item]);

        session.MarkReadyForOrder();
        var updated = CheckoutSessionItem.Create(session.Id, 1, 10, 5, null, 1, 110m, 110m, "IRR", 100m);
        session.ReplaceItems([updated], DateTime.UtcNow, priceChangeDetected: true);

        Assert.Equal(CheckoutStatus.RequiresReview, session.Status);
        Assert.True(session.PriceChangeDetected);
    }

    [Fact]
    public void MarkCartStale_UpdatesStatus()
    {
        var item = CheckoutSessionItem.Create(0, 1, 10, 5, null, 1, 100m, 100m, "IRR");
        var session = CheckoutSession.Create(
            1,
            1,
            null,
            "guest-token",
            1,
            "IRR",
            false,
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow.AddHours(1),
            [item]);

        session.MarkReadyForOrder();
        session.MarkCartStale(DateTime.UtcNow);

        Assert.Equal(CheckoutStatus.RequiresReview, session.Status);
    }

    [Fact]
    public void IsOwnedBy_RejectsCrossCustomerAccess()
    {
        var item = CheckoutSessionItem.Create(0, 1, 10, 5, null, 1, 100m, 100m, "IRR");
        var session = CheckoutSession.Create(
            1,
            1,
            10,
            null,
            1,
            "IRR",
            false,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            [item]);

        Assert.False(session.IsOwnedBy(1, 11, null));
        Assert.True(session.IsOwnedBy(1, 10, null));
    }

    [Fact]
    public void AddressSnapshot_PreservesCustomerSourceId()
    {
        var snapshot = CheckoutAddressSnapshot.Create(
            "Jane",
            "Doe",
            "IR",
            "Tehran",
            "Street 1",
            "1234567890",
            sourceCustomerAddressId: 42);

        Assert.Equal(42, snapshot.SourceCustomerAddressId);
        Assert.Equal("Jane", snapshot.FirstName);
    }
}
