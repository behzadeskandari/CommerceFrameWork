using Commerce.Promotions.Application.Abstractions;
using Commerce.Promotions.Application.Rules;
using Commerce.Promotions.Application.Rules.Actions;
using Commerce.Promotions.Application.Rules.Conditions;
using Commerce.Promotions.Domain.Entities;
using Commerce.Promotions.Domain.Enums;
using Xunit;

namespace Commerce.Tests.Unit.Promotions;

public sealed class PromotionRuleEngineTests
{
    private readonly PromotionRuleEngine _engine = new(
    [
        new MinimumCartSubtotalConditionEvaluator(),
        new MinimumQuantityConditionEvaluator(),
        new CustomerGroupConditionEvaluator(),
        new ProductInCartConditionEvaluator(),
        new CategoryInCartConditionEvaluator(),
        new ProductRestrictionConditionEvaluator(),
        new CategoryRestrictionConditionEvaluator(),
        new StoreRestrictionConditionEvaluator(),
        new UsageLimitRemainingConditionEvaluator(),
        new PerCustomerUsageRemainingConditionEvaluator()
    ],
    [
        new PercentageDiscountActionExecutor(),
        new FixedAmountDiscountActionExecutor(),
        new BuyXGetYActionExecutor(),
        new ApplyLinkedDiscountActionExecutor()
    ]);

    [Fact]
    public void ExpiredPromotion_ReturnsNoEffects()
    {
        var promotion = CreatePromotion(
            endsAtUtc: DateTime.UtcNow.AddDays(-1),
            actions: [LinePercentageAction(10)]);

        var effects = _engine.Evaluate(promotion, BaseState(), PromotionTargetScope.Line);
        Assert.Empty(effects);
    }

    [Fact]
    public void MinimumCartSubtotal_BlocksWhenBelowThreshold()
    {
        var promotion = CreatePromotion(
            conditions: [PromotionCondition.Create(1, PromotionConditionType.MinimumCartSubtotal, """{"minimum":100}""")],
            actions: [CartPercentageAction(10)]);

        var state = BaseState() with { CartSubtotal = 80m };
        var effects = _engine.Evaluate(promotion, state, PromotionTargetScope.Cart);
        Assert.Empty(effects);
    }

    [Fact]
    public void CustomerGroupRestriction_AllowsMatchingGroup()
    {
        var promotion = CreatePromotion(
            conditions: [PromotionCondition.Create(1, PromotionConditionType.CustomerGroup, """{"customerGroupId":3}""")],
            actions: [CartPercentageAction(15)]);

        var state = BaseState() with { CustomerGroupId = 3, CartSubtotal = 200m };
        var effects = _engine.Evaluate(promotion, state, PromotionTargetScope.Cart);
        Assert.Single(effects);
        Assert.Equal(30m, effects[0].Amount);
    }

    [Fact]
    public void PerCustomerUsageLimit_BlocksRepeatCustomer()
    {
        var promotion = CreatePromotion(
            perCustomerUsageLimit: 1,
            actions: [LinePercentageAction(10)]);

        var state = BaseState() with { CustomerId = 42, CustomerPromotionUsageCount = 1 };
        var effects = _engine.Evaluate(promotion, state, PromotionTargetScope.Line);
        Assert.Empty(effects);
    }

    [Fact]
    public void StoreIsolation_BlocksWrongStore()
    {
        var promotion = CreatePromotion(storeId: 2, actions: [LinePercentageAction(10)]);
        var state = BaseState() with { StoreId = 1 };
        var effects = _engine.Evaluate(promotion, state, PromotionTargetScope.Line);
        Assert.Empty(effects);
    }

    [Fact]
    public void ExclusiveCombinationRule_ReturnsSingleBestPromotion()
    {
        var first = new PromotionDiscountEffect(1, "A", 20m, PromotionTargetScope.Line, PromotionCombinationRule.Exclusive, null, 100, 1);
        var second = new PromotionDiscountEffect(2, "B", 10m, PromotionTargetScope.Line, PromotionCombinationRule.Stackable, null, 50, 1);

        var selected = PromotionCombinationSelector.ApplyCombinationRules([second, first]);
        Assert.Single(selected);
        Assert.Equal(1, selected[0].PromotionId);
    }

    [Fact]
    public void SameGroupExclusive_AllowsOnePerGroup()
    {
        var first = new PromotionDiscountEffect(1, "A", 20m, PromotionTargetScope.Cart, PromotionCombinationRule.SameGroupExclusive, "shipping", 100);
        var second = new PromotionDiscountEffect(2, "B", 15m, PromotionTargetScope.Cart, PromotionCombinationRule.SameGroupExclusive, "shipping", 90);
        var third = new PromotionDiscountEffect(3, "C", 5m, PromotionTargetScope.Cart, PromotionCombinationRule.Stackable, null, 80);

        var selected = PromotionCombinationSelector.ApplyCombinationRules([second, first, third]);
        Assert.Equal(2, selected.Count);
        Assert.Contains(selected, x => x.PromotionId == 1);
        Assert.Contains(selected, x => x.PromotionId == 3);
    }

    [Fact]
    public void BuyXGetY_CalculatesFreeUnits()
    {
        var promotion = CreatePromotion(actions:
        [
            PromotionAction.Create(1, PromotionActionType.BuyXGetY, PromotionTargetScope.Line,
                """{"buyQuantity":2,"getQuantity":1,"getDiscountPercent":100,"productIds":[]}""")
        ]);

        var state = BaseState() with
        {
            LineQuantity = 6,
            LineSubtotal = 60m,
            ProductId = 10
        };

        var effects = _engine.Evaluate(promotion, state, PromotionTargetScope.Line);
        Assert.Single(effects);
        Assert.Equal(20m, effects[0].Amount);
    }

    [Fact]
    public void CouponRequired_BlocksWithoutMatchingCode()
    {
        var promotion = CreatePromotion(requiresCoupon: true, couponCode: "SAVE10", actions: [CartPercentageAction(10)]);
        var effects = _engine.Evaluate(promotion, BaseState(), PromotionTargetScope.Cart);
        Assert.Empty(effects);

        var withCoupon = BaseState() with { CouponCode = "save10", CartSubtotal = 100m };
        effects = _engine.Evaluate(promotion, withCoupon, PromotionTargetScope.Cart);
        Assert.Single(effects);
    }

    private static Promotion CreatePromotion(
        int? storeId = null,
        DateTime? endsAtUtc = null,
        int? perCustomerUsageLimit = null,
        bool requiresCoupon = false,
        string? couponCode = null,
        IEnumerable<PromotionCondition>? conditions = null,
        IEnumerable<PromotionAction>? actions = null) =>
        Promotion.Create(
            "Test",
            Guid.NewGuid().ToString("N"),
            null,
            isActive: true,
            startsAtUtc: null,
            endsAtUtc,
            storeId,
            priority: 50,
            PromotionCombinationRule.Stackable,
            combinationGroup: null,
            globalUsageLimit: null,
            perCustomerUsageLimit,
            requiresCoupon,
            couponCode,
            conditions ?? [],
            actions ?? [LinePercentageAction(10)]);

    private static PromotionAction LinePercentageAction(decimal percent) =>
        PromotionAction.Create(1, PromotionActionType.PercentageDiscount, PromotionTargetScope.Line,
            $$"""{"percent":{{percent}}}""");

    private static PromotionAction CartPercentageAction(decimal percent) =>
        PromotionAction.Create(1, PromotionActionType.PercentageDiscount, PromotionTargetScope.Cart,
            $$"""{"percent":{{percent}}}""");

    private static PromotionEvaluationState BaseState() => new()
    {
        StoreId = 1,
        CurrencyCode = "EUR",
        CartSubtotal = 100m,
        TotalQuantity = 2,
        LineQuantity = 2,
        LineSubtotal = 100m,
        Lines =
        [
            new PromotionCartLineState(1, 10, null, 2, 50m, [5])
        ],
        CurrentTimeUtc = DateTime.UtcNow,
        ProductId = 10,
        OfferId = 1,
        LineCategoryIds = [5]
    };
}
