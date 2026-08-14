using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Application.Pricing;
using Commerce.Catalog.Contracts.Pricing;
using Commerce.Customers.Contracts.Customers;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Results;
using Commerce.Pricing.Contracts.AdvancedPricing;
using Commerce.Pricing.Contracts.Pricing;

namespace Commerce.Pricing.Application.Pricing;

/// <summary>
/// Decorates catalog pricing with tier/group adjustments and the authoritative discount pipeline.
/// </summary>
public sealed class DiscountAwarePricingService(
    PricingService innerPricing,
    IProductPricingPipeline pricingPipeline,
    IPriceCalculationService priceCalculationService,
    ICustomerReader customerReader,
    IStoreContext storeContext,
    ICurrentCustomerContext customerContext) : IPricingService, ICatalogPricingReader
{
    public Task<Result<ResolvedPriceDto>> ResolveProductPriceAsync(
        int productId,
        int? currencyId = null,
        CancellationToken cancellationToken = default) =>
        ResolveWithPipelineAsync(
            () => innerPricing.ResolveProductPriceAsync(productId, currencyId, cancellationToken),
            quantity: 1,
            cancellationToken);

    public Task<Result<ResolvedPriceDto>> ResolveVariantPriceAsync(
        int variantId,
        int? currencyId = null,
        CancellationToken cancellationToken = default) =>
        ResolveWithPipelineAsync(
            () => innerPricing.ResolveVariantPriceAsync(variantId, currencyId, cancellationToken),
            quantity: 1,
            cancellationToken);

    public Task<ResolvedPriceDto?> GetOfferPriceAsync(int offerId, CancellationToken cancellationToken = default) =>
        GetOfferPriceAsync(offerId, 1, cancellationToken);

    public async Task<ResolvedPriceDto?> GetOfferPriceAsync(
        int offerId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var basePrice = await innerPricing.GetOfferPriceAsync(offerId, cancellationToken).ConfigureAwait(false);
        return basePrice is null
            ? null
            : await ApplyPipelineAndDiscountsAsync(basePrice, quantity, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<ResolvedPriceDto>> ResolveWithPipelineAsync(
        Func<Task<Result<ResolvedPriceDto>>> resolveBase,
        int quantity,
        CancellationToken cancellationToken)
    {
        var baseResult = await resolveBase().ConfigureAwait(false);
        if (!baseResult.IsSuccess || baseResult.Value is null)
        {
            return baseResult;
        }

        return Result.Success(await ApplyPipelineAndDiscountsAsync(baseResult.Value, quantity, cancellationToken).ConfigureAwait(false));
    }

    private async Task<ResolvedPriceDto> ApplyPipelineAndDiscountsAsync(
        ResolvedPriceDto basePrice,
        int quantity,
        CancellationToken cancellationToken)
    {
        var customerId = customerContext.CustomerId;
        var isGuest = !customerId.HasValue;
        int? customerGroupId = null;

        if (customerId.HasValue)
        {
            var customer = await customerReader.GetByIdAsync(customerId.Value, cancellationToken).ConfigureAwait(false);
            customerGroupId = customer.IsSuccess ? customer.Value?.CustomerGroupId : null;
        }

        var currencyId = storeContext.CurrentCurrencyId ?? 0;
        var pipelineResult = await pricingPipeline.ResolveUnitPriceAsync(
            new ProductPricingContext(
                basePrice.StoreId,
                basePrice.OfferId,
                basePrice.ProductId,
                basePrice.VariantId,
                quantity,
                basePrice.CurrencyCode,
                currencyId,
                basePrice.UnitPrice,
                basePrice.CompareAtPrice,
                customerId,
                customerGroupId,
                DateTime.UtcNow),
            cancellationToken).ConfigureAwait(false);

        var result = await priceCalculationService.CalculateOfferPriceAsync(
            new PriceCalculationContext(
                basePrice.StoreId,
                basePrice.CurrencyCode,
                customerId,
                isGuest,
                customerGroupId,
                basePrice.OfferId,
                basePrice.ProductId,
                basePrice.VariantId,
                quantity,
                pipelineResult.AdjustedUnitPrice,
                pipelineResult.AdjustedUnitPrice * quantity,
                CouponCode: null,
                DateTime.UtcNow),
            cancellationToken).ConfigureAwait(false);

        return basePrice with
        {
            UnitPrice = pipelineResult.AdjustedUnitPrice,
            FinalUnitPrice = result.FinalPrice,
            DiscountAmount = result.DiscountAmount,
            DiscountPercentage = result.DiscountPercentage
        };
    }
}
