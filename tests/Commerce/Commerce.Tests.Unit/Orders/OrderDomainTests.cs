using Commerce.Orders.Application.Abstractions;
using Commerce.Orders.Application.Orders;
using Commerce.Orders.Domain.Entities;
using Commerce.Orders.Domain.Enums;
using Commerce.Orders.Domain.ValueObjects;
using Xunit;

namespace Commerce.Tests.Unit.Orders;

public sealed class OrderDomainTests
{
    [Fact]
    public void CreateFromCheckout_AssignsPendingStatusAndItems()
    {
        var order = CreateSampleOrder();

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal(FulfillmentStatus.Unfulfilled, order.FulfillmentStatus);
        Assert.Single(order.Items);
        Assert.Equal("ORD-2026-000001", order.OrderNumber);
        Assert.Equal(100m, order.GrandTotal);
    }

    [Fact]
    public void CreateFromCheckout_RejectsInconsistentTotals()
    {
        var items = new[] { CreateSampleItem() };

        Assert.Throws<InvalidOperationException>(() =>
            Order.CreateFromCheckout(
                "ORD-2026-000001",
                1,
                1,
                1,
                null,
                "guest@example.com",
                null,
                null,
                "guest-token",
                1,
                "IRR",
                requiresShipping: true,
                CreateSampleBillingAddress(),
                CreateSampleBillingAddress(),
                null,
                null,
                null,
                null,
                subtotal: 100m,
                discountTotal: 0m,
                shippingTotal: 0m,
                taxTotal: 0m,
                grandTotal: 99m,
                items));
    }

    [Fact]
    public void CreateFromCheckout_RejectsEmptyItems()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Order.CreateFromCheckout(
                "ORD-2026-000001",
                1,
                1,
                1,
                null,
                "guest@example.com",
                null,
                null,
                "guest-token",
                1,
                "IRR",
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                0m,
                0m,
                0m,
                0m,
                0m,
                []));
    }

    [Fact]
    public void CreateFromCheckout_RejectsBlankOrderNumber()
    {
        Assert.Throws<ArgumentException>(() =>
            Order.CreateFromCheckout(
                " ",
                1,
                1,
                1,
                null,
                "guest@example.com",
                null,
                null,
                "guest-token",
                1,
                "IRR",
                false,
                null,
                null,
                null,
                null,
                null,
                null,
                100m,
                0m,
                0m,
                0m,
                100m,
                [CreateSampleItem()]));
    }

    [Fact]
    public void Cancel_FromPending_SetsCancelledStatus()
    {
        var order = CreateSampleOrder();

        order.Cancel("Customer requested cancellation.");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(FulfillmentStatus.Cancelled, order.FulfillmentStatus);
        Assert.Contains(order.StatusHistory, x => x.ToStatus == OrderStatus.Cancelled.ToString());
    }

    [Fact]
    public void Cancel_RejectsAlreadyCancelledOrder()
    {
        var order = CreateSampleOrder();
        order.Cancel("First cancellation.");

        Assert.Throws<InvalidOperationException>(() => order.Cancel("Second cancellation."));
    }

    [Fact]
    public void Cancel_RejectsCompletedOrder()
    {
        var order = CreateSampleOrder();
        typeof(Order).GetProperty(nameof(Order.Status))!
            .SetValue(order, OrderStatus.Completed);

        Assert.Throws<InvalidOperationException>(() => order.Cancel("Too late."));
    }

    [Fact]
    public void IsOwnedByCustomer_MatchesCustomerId()
    {
        var order = CreateSampleOrder(customerId: 42);

        Assert.True(order.IsOwnedByCustomer(42));
        Assert.False(order.IsOwnedByCustomer(99));
    }

    [Fact]
    public void IsAccessibleByGuest_RequiresMatchingToken()
    {
        var order = CreateSampleOrder(guestAccessToken: "secret-token");

        Assert.True(order.IsAccessibleByGuest("secret-token"));
        Assert.False(order.IsAccessibleByGuest("wrong-token"));
        Assert.False(order.IsAccessibleByGuest(string.Empty));
    }

    [Fact]
    public void OrderItem_Create_RejectsInvalidQuantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrderItem.Create(0, 1, 1, 10, 5, null, "Product", null, "SKU-1", 0, 25m, 0m, 0m, 0m, 0m, "IRR"));
    }

    [Fact]
    public void StoreOrderNumberSequence_Next_IncrementsSequence()
    {
        var sequence = StoreOrderNumberSequence.Create(1, 2026);

        Assert.Equal(1, sequence.Next());
        Assert.Equal(2, sequence.Next());
    }

    [Fact]
    public async Task OrderNumberGenerator_ProducesYearBasedFormat()
    {
        var repository = new FakeOrderNumberSequenceRepository();
        var generator = new OrderNumberGenerator(repository);
        var year = DateTime.UtcNow.Year;

        var first = await generator.GenerateAsync(1);
        var second = await generator.GenerateAsync(1);

        Assert.Matches($@"^ORD-{year}-\d{{6}}$", first);
        Assert.Matches($@"^ORD-{year}-\d{{6}}$", second);
        Assert.NotEqual(first, second);
    }

    private static Order CreateSampleOrder(int? customerId = null, string? guestAccessToken = "guest-token") =>
        Order.CreateFromCheckout(
            "ORD-2026-000001",
            1,
            1,
            1,
            customerId,
            customerId.HasValue ? null : "guest@example.com",
            customerId.HasValue ? "customer@example.com" : null,
            customerId.HasValue ? "Jane Shopper" : null,
            guestAccessToken,
            1,
            "IRR",
            requiresShipping: true,
            CreateSampleBillingAddress(),
            CreateSampleBillingAddress(),
            null,
            null,
            null,
            null,
            subtotal: 100m,
            discountTotal: 0m,
            shippingTotal: 0m,
            taxTotal: 0m,
            grandTotal: 100m,
            [CreateSampleItem()]);

    private static OrderItem CreateSampleItem() =>
        OrderItem.Create(
            0,
            1,
            1,
            10,
            5,
            null,
            "Sample Product",
            null,
            "SKU-1",
            2,
            50m,
            100m,
            0m,
            0m,
            100m,
            "IRR");

    private static OrderAddressSnapshot CreateSampleBillingAddress() =>
        OrderAddressSnapshot.Create(
            "Jane",
            "Shopper",
            "IR",
            "Tehran",
            "Street 1",
            "1234567890");

    private sealed class FakeOrderNumberSequenceRepository : IOrderNumberSequenceRepository
    {
        private StoreOrderNumberSequence? _sequence;

        public Task<StoreOrderNumberSequence> GetOrCreateAsync(
            int storeId,
            int year,
            CancellationToken cancellationToken = default)
        {
            _sequence ??= StoreOrderNumberSequence.Create(storeId, year);
            return Task.FromResult(_sequence);
        }

        public Task SaveAsync(StoreOrderNumberSequence sequence, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
