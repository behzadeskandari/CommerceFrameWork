using Commerce.Catalog.Contracts.Offers;
using Commerce.Framework.Contracts.Currency;
using Commerce.Framework.Domain.ValueObjects;
using Commerce.Pricing.Application.Abstractions;
using Commerce.Pricing.Contracts.AdvancedPricing;

namespace Commerce.Pricing.Application.AdvancedPricing;

public sealed class ProductPricingPipeline(
    IPricingRepository pricingRepository,
    IOfferTierPriceReader tierPriceReader,
    ICurrencyConverter currencyConverter) : IProductPricingPipeline
{
    public async Task<ProductPricingResult> ResolveUnitPriceAsync(
        ProductPricingContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var currency = Currency.FromCode(context.CurrencyCode);
        var unitPrice = MonetaryRounding.RoundForCalculation(context.BaseUnitPrice);
        var compareAt = context.CompareAtPrice;
        var tierApplied = false;
        var groupApplied = false;
        var converted = false;
        decimal? exchangeRate = null;

        var tierPrice = await tierPriceReader
            .ResolveTierUnitPriceAsync(context.OfferId, context.Quantity, context.CurrencyCode, cancellationToken)
            .ConfigureAwait(false);

        if (tierPrice.HasValue)
        {
            unitPrice = MonetaryRounding.RoundForCalculation(tierPrice.Value);
            tierApplied = true;
        }

        if (context.CustomerGroupId.HasValue)
        {
            var groupPrice = await pricingRepository
                .GetCustomerGroupPriceAsync(
                    context.CustomerGroupId.Value,
                    context.StoreId,
                    context.ProductId,
                    context.VariantId,
                    context.CurrencyId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (groupPrice is not null && groupPrice.IsActive)
            {
                unitPrice = MonetaryRounding.RoundForCalculation(groupPrice.Price);
                groupApplied = true;
            }
        }

        return new ProductPricingResult(
            context.BaseUnitPrice,
            unitPrice,
            compareAt,
            context.CurrencyCode,
            tierApplied,
            groupApplied,
            converted,
            exchangeRate);
    }
}
