using Commerce.Checkout.Contracts.Checkout;
using Commerce.Pricing.Contracts.Pricing;

namespace Commerce.Pricing.Application.Pricing;

public sealed class CheckoutDiscountCalculator(IPriceCalculationService priceCalculationService) : IDiscountCalculator
{
    public async Task<DiscountCalculationResult> CalculateAsync(
        DiscountCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var couponCode = request.CouponCodes.FirstOrDefault();
        var items = request.Items ?? [];

        var context = new CartDiscountCalculationContext(
            request.StoreId,
            request.CartId,
            request.CurrencyCode,
            request.CustomerId,
            request.IsGuest,
            null,
            items.Select(x => new CartDiscountLineContext(
                x.OfferId,
                x.ProductId,
                x.VariantId,
                x.Quantity,
                x.UnitPrice)).ToList(),
            couponCode,
            utcNow);

        var result = await priceCalculationService.CalculateCartAsync(context, cancellationToken).ConfigureAwait(false);

        var lines = result.AppliedDiscounts
            .GroupBy(x => new { x.DiscountId, x.Name, x.Scope, x.CouponCode })
            .Select(g => new DiscountLine(
                g.Key.CouponCode is not null ? $"{g.Key.Name} ({g.Key.CouponCode})" : g.Key.Name,
                g.Sum(x => x.Amount),
                result.CurrencyCode,
                g.Key.DiscountId,
                g.First().OfferId,
                g.Key.CouponCode,
                g.Key.Scope))
            .ToList();

        return new DiscountCalculationResult(result.DiscountTotal, result.CurrencyCode, lines);
    }
}
