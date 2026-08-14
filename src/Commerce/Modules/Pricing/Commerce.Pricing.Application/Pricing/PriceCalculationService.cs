using Commerce.Pricing.Application.Abstractions;
using Commerce.Pricing.Application.Pricing;
using Commerce.Promotions.Contracts.Pricing;
using Commerce.Pricing.Contracts.Pricing;
using Commerce.Pricing.Domain.Entities;
using Commerce.Pricing.Domain.Enums;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Pricing.Application.Pricing;

public sealed class PriceCalculationService(
    IPricingRepository repository,
    IProductCategoryLookup categoryLookup,
    ICouponValidationService couponValidationService,
    IPromotionEvaluationService promotionEvaluationService) : IPriceCalculationService
{
    public async Task<PriceCalculationResult> CalculateOfferPriceAsync(
        PriceCalculationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discounts = await repository.GetActiveDiscountsAsync(context.StoreId, context.CurrentTimeUtc, cancellationToken)
            .ConfigureAwait(false);

        var categoryMap = await categoryLookup
            .GetCategoryIdsByProductIdsAsync([context.ProductId], cancellationToken)
            .ConfigureAwait(false);

        var categoryIds = categoryMap.GetValueOrDefault(context.ProductId) ?? [];
        var lineSubtotal = Money.Create(context.BaseUnitPrice, Currency.FromCode(context.CurrencyCode))
            .Multiply(context.Quantity).Amount;

        var lineDiscounts = DiscountCalculationEngine.SelectApplicableLineDiscounts(
            discounts,
            context.OfferId,
            context.ProductId,
            context.VariantId,
            categoryIds,
            context.Quantity,
            lineSubtotal,
            context.CartSubtotal,
            context.StoreId,
            context.CustomerId,
            context.IsGuest,
            context.CustomerGroupId,
            context.CurrencyCode,
            context.CurrentTimeUtc);

        var couponDiscount = await ResolveCouponDiscountAsync(context, discounts, lineSubtotal, cancellationToken)
            .ConfigureAwait(false);

        var allDiscounts = MergeCouponDiscount(couponDiscount, lineDiscounts);

        var (finalLineAmount, applied) = DiscountCalculationEngine.ApplyDiscountsToAmount(
            lineSubtotal,
            allDiscounts,
            context.CurrencyCode);

        var promotionEffects = await promotionEvaluationService
            .EvaluateLinePromotionsAsync(PromotionPricingIntegrator.BuildLineContext(context, categoryIds), cancellationToken)
            .ConfigureAwait(false);
        var promotionDiscount = PromotionPricingIntegrator.SumPromotionDiscounts(promotionEffects);
        finalLineAmount = Math.Max(0m, finalLineAmount - promotionDiscount);

        var discountTotal = lineSubtotal - finalLineAmount;
        var finalUnit = context.Quantity > 0
            ? Money.Create(finalLineAmount / context.Quantity, Currency.FromCode(context.CurrencyCode)).Amount
            : context.BaseUnitPrice;

        var appliedDtos = applied.Select(x => new AppliedDiscountDto(
            x.Discount.Id,
            x.Discount.Name,
            x.Amount,
            context.CurrencyCode,
            "Line",
            context.OfferId,
            couponDiscount?.Id == x.Discount.Id ? context.CouponCode : null)).ToList();
        appliedDtos.AddRange(PromotionPricingIntegrator.MapPromotionEffects(
            promotionEffects, context.CurrencyCode, "Line", context.OfferId));

        decimal? discountPercentage = lineSubtotal > 0
            ? Math.Round(discountTotal / lineSubtotal * 100m, 2, MidpointRounding.ToEven)
            : null;

        return new PriceCalculationResult(
            context.BaseUnitPrice,
            discountTotal,
            finalUnit,
            context.CurrencyCode,
            appliedDtos,
            discountPercentage);
    }

    public async Task<CartDiscountCalculationResult> CalculateCartAsync(
        CartDiscountCalculationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discounts = await repository.GetActiveDiscountsAsync(context.StoreId, context.CurrentTimeUtc, cancellationToken)
            .ConfigureAwait(false);

        var productIds = context.Lines.Select(x => x.ProductId).Distinct().ToList();
        var categoryMap = await categoryLookup
            .GetCategoryIdsByProductIdsAsync(productIds, cancellationToken)
            .ConfigureAwait(false);

        var lineResults = new List<CartLineDiscountResult>();
        var allApplied = new List<AppliedDiscountDto>();
        var currency = Currency.FromCode(context.CurrencyCode);
        var subtotal = Money.Zero(currency);

        foreach (var line in context.Lines)
        {
            var categoryIds = categoryMap.GetValueOrDefault(line.ProductId) ?? [];
            var lineSubtotal = Money.Create(line.UnitPrice, currency).Multiply(line.Quantity).Amount;
            subtotal = subtotal.Add(Money.Create(lineSubtotal, currency));

            var lineDiscounts = DiscountCalculationEngine.SelectApplicableLineDiscounts(
                discounts,
                line.OfferId,
                line.ProductId,
                line.VariantId,
                categoryIds,
                line.Quantity,
                lineSubtotal,
                subtotal.Amount,
                context.StoreId,
                context.CustomerId,
                context.IsGuest,
                context.CustomerGroupId,
                context.CurrencyCode,
                context.CurrentTimeUtc);

            var (finalLineAmount, applied) = DiscountCalculationEngine.ApplyDiscountsToAmount(
                lineSubtotal,
                lineDiscounts,
                context.CurrencyCode);

            var lineDiscountTotal = lineSubtotal - finalLineAmount;
            var finalUnit = line.Quantity > 0
                ? Money.Create(finalLineAmount / line.Quantity, currency).Amount
                : line.UnitPrice;

            var lineApplied = applied.Select(x => new AppliedDiscountDto(
                x.Discount.Id,
                x.Discount.Name,
                x.Amount,
                context.CurrencyCode,
                "Line",
                line.OfferId)).ToList();

            allApplied.AddRange(lineApplied);

            lineResults.Add(new CartLineDiscountResult(
                line.OfferId,
                line.UnitPrice,
                finalUnit,
                finalLineAmount,
                lineDiscountTotal,
                lineApplied));
        }

        var discountedSubtotal = subtotal.Amount - lineResults.Sum(x => x.LineDiscountTotal);
        var couponDiscount = await ResolveCouponDiscountForCartAsync(context, discounts, subtotal.Amount, cancellationToken)
            .ConfigureAwait(false);

        var cartDiscounts = DiscountCalculationEngine.SelectApplicableCartDiscounts(
            discounts,
            subtotal.Amount,
            context.StoreId,
            context.CustomerId,
            context.IsGuest,
            context.CustomerGroupId,
            context.CurrencyCode,
            context.CurrentTimeUtc,
            context.Lines.Sum(x => x.Quantity));

        var mergedCartDiscounts = MergeCouponDiscount(couponDiscount, cartDiscounts);
        var (finalCartAmount, cartApplied) = DiscountCalculationEngine.ApplyDiscountsToAmount(
            discountedSubtotal,
            mergedCartDiscounts,
            context.CurrencyCode);

        var cartDiscountSum = discountedSubtotal - finalCartAmount;
        var cartPromotionEffects = await promotionEvaluationService
            .EvaluateCartPromotionsAsync(
                PromotionPricingIntegrator.BuildCartContext(context, subtotal.Amount, categoryMap),
                cancellationToken)
            .ConfigureAwait(false);
        var cartPromotionDiscount = PromotionPricingIntegrator.SumPromotionDiscounts(cartPromotionEffects);
        var totalDiscount = lineResults.Sum(x => x.LineDiscountTotal) + cartDiscountSum + cartPromotionDiscount;

        allApplied.AddRange(cartApplied.Select(x => new AppliedDiscountDto(
            x.Discount.Id,
            x.Discount.Name,
            x.Amount,
            context.CurrencyCode,
            "Cart",
            null,
            couponDiscount?.Id == x.Discount.Id ? context.CouponCode : null)));
        allApplied.AddRange(PromotionPricingIntegrator.MapPromotionEffects(cartPromotionEffects, context.CurrencyCode, "Cart"));

        var grandTotal = subtotal.Amount - totalDiscount;
        if (grandTotal < 0)
        {
            grandTotal = 0m;
        }

        return new CartDiscountCalculationResult(
            subtotal.Amount,
            totalDiscount,
            grandTotal,
            context.CurrencyCode,
            allApplied,
            lineResults);
    }

    private async Task<Discount?> ResolveCouponDiscountAsync(
        PriceCalculationContext context,
        IReadOnlyList<Discount> discounts,
        decimal lineSubtotal,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.CouponCode))
        {
            return null;
        }

        var validation = await couponValidationService.ValidateAsync(
            new CouponValidationRequest(
                context.CouponCode,
                context.StoreId,
                context.CurrencyCode,
                context.CustomerId,
                context.IsGuest,
                context.CustomerGroupId,
                context.CartSubtotal,
                context.CurrentTimeUtc),
            cancellationToken).ConfigureAwait(false);

        if (!validation.IsValid || !validation.DiscountId.HasValue)
        {
            return null;
        }

        return discounts.FirstOrDefault(d => d.Id == validation.DiscountId.Value);
    }

    private async Task<Discount?> ResolveCouponDiscountForCartAsync(
        CartDiscountCalculationContext context,
        IReadOnlyList<Discount> discounts,
        decimal cartSubtotal,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.CouponCode))
        {
            return null;
        }

        var validation = await couponValidationService.ValidateAsync(
            new CouponValidationRequest(
                context.CouponCode,
                context.StoreId,
                context.CurrencyCode,
                context.CustomerId,
                context.IsGuest,
                context.CustomerGroupId,
                cartSubtotal,
                context.CurrentTimeUtc),
            cancellationToken).ConfigureAwait(false);

        if (!validation.IsValid || !validation.DiscountId.HasValue)
        {
            return null;
        }

        return discounts.FirstOrDefault(d => d.Id == validation.DiscountId.Value);
    }

    private static IReadOnlyList<Discount> MergeCouponDiscount(Discount? couponDiscount, IReadOnlyList<Discount> others)
    {
        if (couponDiscount is null)
        {
            return others;
        }

        if (others.Any(d => d.Id == couponDiscount.Id))
        {
            return others;
        }

        return others.Concat([couponDiscount]).OrderByDescending(d => d.Priority).ToList();
    }
}
