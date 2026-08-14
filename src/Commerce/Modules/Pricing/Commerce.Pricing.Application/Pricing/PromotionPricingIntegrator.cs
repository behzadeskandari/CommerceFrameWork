using Commerce.Promotions.Contracts.Pricing;
using Commerce.Pricing.Contracts.Pricing;

namespace Commerce.Pricing.Application.Pricing;

internal static class PromotionPricingIntegrator
{
    public static decimal SumPromotionDiscounts(IReadOnlyList<PromotionDiscountEffect> effects) =>
        effects.Sum(x => x.Amount);

    public static IReadOnlyList<AppliedDiscountDto> MapPromotionEffects(
        IReadOnlyList<PromotionDiscountEffect> effects,
        string currencyCode,
        string scope,
        int? offerId = null) =>
        effects.Select(x => new AppliedDiscountDto(
            x.PromotionId,
            x.Name,
            x.Amount,
            currencyCode,
            scope,
            offerId)).ToList();

    public static PromotionEvaluationContext BuildLineContext(PriceCalculationContext context, IReadOnlyList<int> categoryIds) =>
        new(
            context.StoreId,
            context.CurrencyCode,
            context.CustomerId,
            context.CustomerGroupId,
            context.IsGuest,
            context.CartSubtotal,
            context.Quantity,
            [],
            context.OfferId,
            context.ProductId,
            context.VariantId,
            context.Quantity,
            context.BaseUnitPrice * context.Quantity,
            categoryIds,
            context.CouponCode,
            context.CurrentTimeUtc);

    public static PromotionEvaluationContext BuildCartContext(
        CartDiscountCalculationContext context,
        decimal cartSubtotal,
        IReadOnlyDictionary<int, IReadOnlyList<int>> categoryMap) =>
        new(
            context.StoreId,
            context.CurrencyCode,
            context.CustomerId,
            context.CustomerGroupId,
            context.IsGuest,
            cartSubtotal,
            context.Lines.Sum(x => x.Quantity),
            context.Lines.Select(line => new PromotionCartLineContext(
                line.OfferId,
                line.ProductId,
                line.VariantId,
                line.Quantity,
                line.UnitPrice,
                categoryMap.GetValueOrDefault(line.ProductId) ?? [])).ToList(),
            null,
            null,
            null,
            0,
            0,
            [],
            context.CouponCode,
            context.CurrentTimeUtc);
}
