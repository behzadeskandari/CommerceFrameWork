using Commerce.Pricing.Application.Pricing;
using Commerce.Pricing.Domain.Entities;
using Commerce.Pricing.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Unit.Pricing;

public sealed class DiscountCalculationEngineTests
{
    [Fact]
    public void PercentageDiscount_ReducesPriceCorrectly()
    {
        var discount = CreateDiscount(DiscountType.Percentage, 20m, scope: DiscountApplicationScope.Line);
        var amount = DiscountCalculationEngine.CalculateDiscountAmount(discount, 100m, "EUR");
        Assert.Equal(20m, amount);
    }

    [Fact]
    public void FixedDiscount_ReducesPriceCorrectly()
    {
        var discount = CreateDiscount(DiscountType.FixedAmount, 15m, "EUR", scope: DiscountApplicationScope.Line);
        var amount = DiscountCalculationEngine.CalculateDiscountAmount(discount, 100m, "EUR");
        Assert.Equal(15m, amount);
    }

    [Fact]
    public void MaximumDiscount_CapsPercentageDiscount()
    {
        var discount = CreateDiscount(
            DiscountType.Percentage,
            20m,
            maximumDiscountAmount: 10m,
            scope: DiscountApplicationScope.Line);
        var amount = DiscountCalculationEngine.CalculateDiscountAmount(discount, 100m, "EUR");
        Assert.Equal(10m, amount);
    }

    [Fact]
    public void MinimumCartSubtotal_BlocksDiscountBelowThreshold()
    {
        var discount = CreateDiscount(
            DiscountType.FixedAmount,
            20m,
            "EUR",
            minimumCartSubtotal: 100m,
            scope: DiscountApplicationScope.Cart);
        discount.LoadTargets([DiscountTarget.Create(1, DiscountTargetType.Cart, 0)]);

        var applicable = DiscountCalculationEngine.SelectApplicableCartDiscounts(
            [discount],
            cartSubtotal: 79m,
            storeId: 1,
            customerId: null,
            isGuest: true,
            customerGroupId: null,
            currencyCode: "EUR",
            utcNow: DateTime.UtcNow,
            totalQuantity: 1);

        Assert.Empty(applicable);
    }

    [Fact]
    public void MinimumCartSubtotal_AllowsDiscountAtThreshold()
    {
        var discount = CreateDiscount(
            DiscountType.FixedAmount,
            20m,
            "EUR",
            minimumCartSubtotal: 100m,
            scope: DiscountApplicationScope.Cart);
        discount.LoadTargets([DiscountTarget.Create(1, DiscountTargetType.Cart, 0)]);

        var applicable = DiscountCalculationEngine.SelectApplicableCartDiscounts(
            [discount],
            cartSubtotal: 100m,
            storeId: 1,
            customerId: null,
            isGuest: true,
            customerGroupId: null,
            currencyCode: "EUR",
            utcNow: DateTime.UtcNow,
            totalQuantity: 1);

        Assert.Single(applicable);
    }

    [Fact]
    public void MinimumQuantity_BlocksDiscountBelowThreshold()
    {
        var discount = CreateDiscount(
            DiscountType.Percentage,
            15m,
            minimumQuantity: 10,
            scope: DiscountApplicationScope.Line);
        discount.LoadTargets([DiscountTarget.Create(1, DiscountTargetType.Offer, 5)]);

        var applicable = DiscountCalculationEngine.SelectApplicableLineDiscounts(
            [discount],
            offerId: 5,
            productId: 1,
            variantId: null,
            productCategoryIds: [],
            quantity: 5,
            lineSubtotal: 50m,
            cartSubtotal: 50m,
            storeId: 1,
            customerId: null,
            isGuest: true,
            customerGroupId: null,
            currencyCode: "EUR",
            utcNow: DateTime.UtcNow);

        Assert.Empty(applicable);
    }

    [Fact]
    public void StoreRestriction_ExcludesOtherStores()
    {
        var discount = CreateDiscount(DiscountType.Percentage, 10m, storeId: 2, scope: DiscountApplicationScope.Line);
        discount.LoadTargets([DiscountTarget.Create(1, DiscountTargetType.Product, 1)]);

        var applicable = DiscountCalculationEngine.SelectApplicableLineDiscounts(
            [discount],
            offerId: 1,
            productId: 1,
            variantId: null,
            productCategoryIds: [],
            quantity: 1,
            lineSubtotal: 100m,
            cartSubtotal: 100m,
            storeId: 1,
            customerId: null,
            isGuest: true,
            customerGroupId: null,
            currencyCode: "EUR",
            utcNow: DateTime.UtcNow);

        Assert.Empty(applicable);
    }

    [Fact]
    public void FixedDiscount_RequiresMatchingCurrency()
    {
        var discount = CreateDiscount(DiscountType.FixedAmount, 10m, "EUR", scope: DiscountApplicationScope.Line);
        var amount = DiscountCalculationEngine.CalculateDiscountAmount(discount, 100m, "USD");
        Assert.Equal(0m, amount);
    }

    [Fact]
    public void GuestRestriction_ExcludesAuthenticatedCustomer()
    {
        var discount = CreateDiscount(
            DiscountType.Percentage,
            10m,
            customerEligibility: CustomerEligibility.Guest,
            scope: DiscountApplicationScope.Line);
        discount.LoadTargets([DiscountTarget.Create(1, DiscountTargetType.Product, 1)]);

        var applicable = DiscountCalculationEngine.SelectApplicableLineDiscounts(
            [discount],
            offerId: 1,
            productId: 1,
            variantId: null,
            productCategoryIds: [],
            quantity: 1,
            lineSubtotal: 100m,
            cartSubtotal: 100m,
            storeId: 1,
            customerId: 42,
            isGuest: false,
            customerGroupId: null,
            currencyCode: "EUR",
            utcNow: DateTime.UtcNow);

        Assert.Empty(applicable);
    }

    [Fact]
    public void OfferTargeting_HasHigherSpecificityThanProduct()
    {
        var offerDiscount = CreateDiscount(DiscountType.Percentage, 20m, priority: 50, scope: DiscountApplicationScope.Line);
        offerDiscount.LoadTargets([DiscountTarget.Create(1, DiscountTargetType.Offer, 10)]);

        var productDiscount = CreateDiscount(DiscountType.Percentage, 10m, priority: 100, scope: DiscountApplicationScope.Line);
        productDiscount.LoadTargets([DiscountTarget.Create(1, DiscountTargetType.Product, 1)]);

        var applicable = DiscountCalculationEngine.SelectApplicableLineDiscounts(
            [productDiscount, offerDiscount],
            offerId: 10,
            productId: 1,
            variantId: null,
            productCategoryIds: [],
            quantity: 1,
            lineSubtotal: 100m,
            cartSubtotal: 100m,
            storeId: 1,
            customerId: null,
            isGuest: true,
            customerGroupId: null,
            currencyCode: "EUR",
            utcNow: DateTime.UtcNow);

        Assert.Equal(offerDiscount, applicable[0]);
    }

    [Fact]
    public void NonStackable_OnlyAppliesHighestPriorityDiscount()
    {
        var high = CreateDiscount(DiscountType.Percentage, 20m, priority: 100, stacking: StackingMode.NonStackable, scope: DiscountApplicationScope.Line);
        high.LoadTargets([DiscountTarget.Create(1, DiscountTargetType.Product, 1)]);

        var low = CreateDiscount(DiscountType.Percentage, 10m, priority: 50, stacking: StackingMode.NonStackable, scope: DiscountApplicationScope.Line);
        low.LoadTargets([DiscountTarget.Create(1, DiscountTargetType.Product, 1)]);

        var (final, applied) = DiscountCalculationEngine.ApplyDiscountsToAmount(
            100m,
            DiscountCalculationEngine.SelectApplicableLineDiscounts(
                [low, high],
                offerId: 1,
                productId: 1,
                variantId: null,
                productCategoryIds: [],
                quantity: 1,
                lineSubtotal: 100m,
                cartSubtotal: 100m,
                storeId: 1,
                customerId: null,
                isGuest: true,
                customerGroupId: null,
                currencyCode: "EUR",
                utcNow: DateTime.UtcNow),
            "EUR");

        Assert.Equal(80m, final);
        Assert.Single(applied);
        Assert.Equal(20m, applied[0].Amount);
    }

    [Fact]
    public void Stackable_AppliesSequentialCompoundDiscount()
    {
        var first = CreateDiscount(DiscountType.Percentage, 20m, priority: 100, stacking: StackingMode.Stackable, scope: DiscountApplicationScope.Line);
        first.LoadTargets([DiscountTarget.Create(1, DiscountTargetType.Product, 1)]);

        var second = CreateDiscount(DiscountType.Percentage, 10m, priority: 50, stacking: StackingMode.Stackable, scope: DiscountApplicationScope.Line);
        second.LoadTargets([DiscountTarget.Create(1, DiscountTargetType.Product, 1)]);

        var (final, applied) = DiscountCalculationEngine.ApplyDiscountsToAmount(
            100m,
            [first, second],
            "EUR");

        Assert.Equal(72m, final);
        Assert.Equal(2, applied.Count);
    }

    [Fact]
    public void CouponNormalization_IsCaseInsensitive()
    {
        Assert.Equal("WELCOME20", Coupon.NormalizeCode("welcome20"));
        Assert.Equal("WELCOME20", Coupon.NormalizeCode("Welcome20"));
    }

    private static Discount CreateDiscount(
        DiscountType type,
        decimal value,
        string? currency = null,
        int priority = 50,
        int? storeId = null,
        decimal? maximumDiscountAmount = null,
        decimal? minimumCartSubtotal = null,
        int? minimumQuantity = null,
        CustomerEligibility customerEligibility = CustomerEligibility.All,
        StackingMode stacking = StackingMode.NonStackable,
        DiscountApplicationScope scope = DiscountApplicationScope.Line)
    {
        var discount = Discount.Create(
            "Test Discount",
            Guid.NewGuid().ToString("N"),
            null,
            type,
            value,
            currency,
            priority,
            isActive: true,
            startsAtUtc: null,
            endsAtUtc: null,
            storeId,
            stacking,
            maximumDiscountAmount,
            minimumCartSubtotal,
            minimumQuantity,
            customerEligibility,
            specificCustomerId: null,
            customerGroupId: null,
            scope,
            []);

        return discount;
    }
}
