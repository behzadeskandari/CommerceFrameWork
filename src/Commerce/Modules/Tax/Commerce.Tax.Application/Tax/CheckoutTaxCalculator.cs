using Commerce.Catalog.Contracts.Products;
using Commerce.Checkout.Contracts.Checkout;
using Commerce.Customers.Contracts.Customers;
using Commerce.Pricing.Contracts.Pricing;
using Commerce.Tax.Application.Abstractions;
using Commerce.Tax.Contracts.Tax;

namespace Commerce.Tax.Application.Tax;

public sealed class CheckoutTaxCalculator(
    ITaxCalculationService calculationService,
    IPriceCalculationService priceCalculationService,
    IProductReader productReader,
    ICustomerReader customerReader,
    TaxSettings taxSettings) : ITaxCalculator
{
    public async Task<TaxCalculationResult> CalculateAsync(
        TaxCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pricesIncludeTax = await taxSettings.GetPricesIncludeTaxAsync(request.StoreId, cancellationToken).ConfigureAwait(false);
        var isExempt = await ResolveCustomerExemptionAsync(request.CustomerId, cancellationToken).ConfigureAwait(false);

        var taxAddress = ResolveTaxAddress(request);
        if (taxAddress is null)
        {
            return new TaxCalculationResult(0m, 0m, 0m, request.CurrencyCode, [], [], pricesIncludeTax);
        }

        var lineDiscounts = await ResolveLineDiscountsAsync(request, cancellationToken).ConfigureAwait(false);
        var taxLines = new List<TaxCalculationLineContext>();

        foreach (var item in request.Items)
        {
            var productResult = await productReader.GetByIdAsync(item.ProductId, cancellationToken).ConfigureAwait(false);
            var taxCategoryId = productResult.IsSuccess ? productResult.Value?.TaxCategoryId : null;
            var lineDiscount = lineDiscounts.GetValueOrDefault(item.OfferId, 0m);
            var lineSubtotal = item.LineSubtotal > 0 ? item.LineSubtotal : item.UnitPrice * item.Quantity;
            var taxableAmount = Math.Max(0m, lineSubtotal - lineDiscount);

            taxLines.Add(new TaxCalculationLineContext(
                item.OfferId,
                item.ProductId,
                item.VariantId,
                item.Quantity,
                item.UnitPrice,
                lineSubtotal,
                lineDiscount,
                taxableAmount,
                item.ProductType,
                taxCategoryId));
        }

        var context = new TaxCalculationContext(
            request.StoreId,
            request.CartId,
            request.CurrencyCode,
            request.CustomerId,
            request.IsGuest,
            isExempt,
            pricesIncludeTax,
            taxAddress.Country,
            taxAddress.StateProvince,
            taxAddress.PostalCode,
            request.ShippingTotal,
            request.RequiresShipping,
            taxLines);

        var result = await calculationService.CalculateAsync(context, cancellationToken).ConfigureAwait(false);

        return new TaxCalculationResult(
            result.TaxTotal,
            result.ProductTaxTotal,
            result.ShippingTaxTotal,
            result.CurrencyCode,
            result.Lines.Select(x => new TaxLine(
                x.Name,
                x.TaxAmount,
                result.CurrencyCode,
                x.RatePercentage,
                x.IsShippingTax,
                x.TaxableAmount)).ToList(),
            result.LineItems.Select(x => new TaxLineItemResult(
                x.OfferId,
                x.TaxableAmount,
                x.TaxAmount,
                x.TaxCategoryId,
                x.TaxCategoryName,
                x.RatePercentage)).ToList(),
            result.PricesIncludeTax);
    }

    private async Task<bool> ResolveCustomerExemptionAsync(int? customerId, CancellationToken cancellationToken)
    {
        if (!customerId.HasValue)
        {
            return false;
        }

        var customer = await customerReader.GetByIdAsync(customerId.Value, cancellationToken).ConfigureAwait(false);
        return customer.IsSuccess && customer.Value?.IsTaxExempt == true;
    }

    private async Task<Dictionary<int, decimal>> ResolveLineDiscountsAsync(
        TaxCalculationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return [];
        }

        var cartResult = await priceCalculationService.CalculateCartAsync(
            new CartDiscountCalculationContext(
                request.StoreId,
                request.CartId,
                request.CurrencyCode,
                request.CustomerId,
                request.IsGuest,
                null,
                request.Items.Select(x => new CartDiscountLineContext(
                    x.OfferId,
                    x.ProductId,
                    x.VariantId,
                    x.Quantity,
                    x.UnitPrice)).ToList(),
                request.CouponCodes.FirstOrDefault(),
                DateTime.UtcNow),
            cancellationToken).ConfigureAwait(false);

        return cartResult.LineResults.ToDictionary(x => x.OfferId, x => x.LineDiscountTotal);
    }

    private static CheckoutAddressDto? ResolveTaxAddress(TaxCalculationRequest request)
    {
        if (request.RequiresShipping && request.ShippingAddress is not null)
        {
            return request.ShippingAddress;
        }

        return request.BillingAddress;
    }
}
