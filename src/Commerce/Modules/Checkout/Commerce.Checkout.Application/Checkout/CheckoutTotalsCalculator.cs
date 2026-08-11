using Commerce.Checkout.Contracts.Checkout;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Checkout.Application.Checkout;

public sealed class CheckoutTotalsCalculator(
    ITaxCalculator taxCalculator,
    IDiscountCalculator discountCalculator) : ICheckoutTotalsCalculator
{
    public async Task<CheckoutTotalResult> CalculateAsync(
        CheckoutTotalContext context,
        CancellationToken cancellationToken = default)
    {
        var discount = await discountCalculator.CalculateAsync(
            new DiscountCalculationRequest(
                context.StoreId,
                context.CustomerId,
                0,
                context.CurrencyCode,
                context.Subtotal,
                context.CouponCodes),
            cancellationToken).ConfigureAwait(false);

        var tax = await taxCalculator.CalculateAsync(
            new TaxCalculationRequest(
                context.StoreId,
                context.CurrencyCode,
                context.CustomerId,
                context.BillingAddress,
                context.ShippingAddress,
                context.Subtotal,
                context.Items),
            cancellationToken).ConfigureAwait(false);

        var currency = Currency.FromCode(context.CurrencyCode);
        var subtotal = Money.Create(context.Subtotal, currency);
        var shipping = Money.Create(context.ShippingTotal, currency);
        var discountMoney = Money.Create(discount.DiscountTotal, currency);
        var taxMoney = Money.Create(tax.TaxTotal, currency);
        var grand = subtotal.Add(shipping).Add(taxMoney).Subtract(discountMoney);

        return new CheckoutTotalResult(
            subtotal.Amount,
            discount.DiscountTotal,
            shipping.Amount,
            tax.TaxTotal,
            grand.Amount,
            currency.Code);
    }
}
