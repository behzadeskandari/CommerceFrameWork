using Commerce.Checkout.Contracts.Checkout;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Checkout.Application.Checkout;

public sealed class CheckoutTotalsCalculator(
    ITaxCalculator taxCalculator,
    IDiscountCalculator discountCalculator,
    ICheckoutWalletCalculator walletCalculator) : ICheckoutTotalsCalculator
{
    public async Task<CheckoutTotalResult> CalculateAsync(
        CheckoutTotalContext context,
        CancellationToken cancellationToken = default)
    {
        var discount = await discountCalculator.CalculateAsync(
            new DiscountCalculationRequest(
                context.StoreId,
                context.CustomerId,
                context.CartId,
                context.CurrencyCode,
                context.Subtotal,
                context.CouponCodes,
                context.IsGuest,
                context.Items.Select(x => new DiscountLineItem(
                    x.OfferId,
                    x.ProductId,
                    x.VariantId,
                    x.Quantity,
                    x.UnitPrice)).ToList()),
            cancellationToken).ConfigureAwait(false);

        var tax = await taxCalculator.CalculateAsync(
            new TaxCalculationRequest(
                context.StoreId,
                context.CartId,
                context.CurrencyCode,
                context.CustomerId,
                context.BillingAddress,
                context.ShippingAddress,
                context.Subtotal,
                context.Items,
                context.ShippingTotal,
                context.CouponCodes,
                context.IsGuest,
                context.RequiresShipping),
            cancellationToken).ConfigureAwait(false);

        var currency = Currency.FromCode(context.CurrencyCode);
        var subtotal = Money.Create(context.Subtotal, currency);
        var shipping = Money.Create(context.ShippingTotal, currency);
        var discountMoney = Money.Create(discount.DiscountTotal, currency);
        var taxMoney = Money.Create(tax.TaxTotal, currency);
        var payable = subtotal.Add(shipping).Add(taxMoney).Subtract(discountMoney);

        var wallet = await walletCalculator.CalculateAsync(
            new CheckoutWalletContext(
                context.StoreId,
                context.CustomerId,
                context.CurrencyCode,
                payable.Amount,
                context.AppliedGiftCardCode,
                context.AppliedStoreCreditAmount),
            cancellationToken).ConfigureAwait(false);

        return new CheckoutTotalResult(
            subtotal.Amount,
            discount.DiscountTotal,
            shipping.Amount,
            tax.TaxTotal,
            wallet.AdjustedGrandTotal,
            currency.Code,
            tax.ProductTaxTotal,
            tax.ShippingTaxTotal,
            tax.PricesIncludeTax,
            tax.Lines,
            tax.LineItems,
            wallet.GiftCardApplied,
            wallet.StoreCreditApplied);
    }
}
