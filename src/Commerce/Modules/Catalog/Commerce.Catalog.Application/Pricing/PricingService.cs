using Commerce.Catalog.Application.Abstractions;
using Commerce.Catalog.Contracts.Products;
using Commerce.Catalog.Contracts.Pricing;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Inventory.Contracts.Inventory;

namespace Commerce.Catalog.Application.Pricing;

public sealed class PricingService(
    IProductRepository productRepository,
    IProductVariantRepository variantRepository,
    IProductOfferRepository offerRepository,
    IStorefrontInventoryReader inventoryReader,
    IStoreContext storeContext) : IPricingService, ICatalogPricingReader
{
    public async Task<Result<ResolvedPriceDto>> ResolveProductPriceAsync(
        int productId,
        int? currencyId = null,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Deleted)
        {
            return Result.Failure<ResolvedPriceDto>(Error.NotFound($"Product '{productId}' was not found."));
        }

        var storeId = storeContext.CurrentStoreId;
        if (!storeId.HasValue)
        {
            return Result.Failure<ResolvedPriceDto>(Error.Validation("Store context is required to resolve price."));
        }

        var resolvedCurrencyId = currencyId ?? storeContext.CurrentCurrencyId;
        if (!resolvedCurrencyId.HasValue)
        {
            return Result.Failure<ResolvedPriceDto>(Error.Validation("Currency context is required to resolve price."));
        }

        var offer = await offerRepository.FindActiveOfferAsync(
            productId,
            variantId: null,
            storeId.Value,
            resolvedCurrencyId.Value,
            DateTime.UtcNow,
            cancellationToken).ConfigureAwait(false);

        if (offer is null)
        {
            return Result.Failure<ResolvedPriceDto>(
                Error.NotFound($"No active offer found for product '{productId}'."));
        }

        return Result.Success(await MapOfferAsync(offer, cancellationToken).ConfigureAwait(false));
    }

    public async Task<Result<ResolvedPriceDto>> ResolveVariantPriceAsync(
        int variantId,
        int? currencyId = null,
        CancellationToken cancellationToken = default)
    {
        var variant = await variantRepository.GetByIdAsync(variantId, cancellationToken).ConfigureAwait(false);
        if (variant is null)
        {
            return Result.Failure<ResolvedPriceDto>(Error.NotFound($"Variant '{variantId}' was not found."));
        }

        var storeId = storeContext.CurrentStoreId;
        if (!storeId.HasValue)
        {
            return Result.Failure<ResolvedPriceDto>(Error.Validation("Store context is required to resolve price."));
        }

        var resolvedCurrencyId = currencyId ?? storeContext.CurrentCurrencyId;
        if (!resolvedCurrencyId.HasValue)
        {
            return Result.Failure<ResolvedPriceDto>(Error.Validation("Currency context is required to resolve price."));
        }

        var offer = await offerRepository.FindActiveOfferAsync(
            variant.ProductId,
            variantId,
            storeId.Value,
            resolvedCurrencyId.Value,
            DateTime.UtcNow,
            cancellationToken).ConfigureAwait(false);

        if (offer is null)
        {
            return Result.Failure<ResolvedPriceDto>(
                Error.NotFound($"No active offer found for variant '{variantId}'."));
        }

        return Result.Success(await MapOfferAsync(offer, cancellationToken).ConfigureAwait(false));
    }

    public async Task<ResolvedPriceDto?> GetOfferPriceAsync(int offerId, CancellationToken cancellationToken = default)
    {
        var offer = await offerRepository.GetByIdAsync(offerId, cancellationToken).ConfigureAwait(false);
        return offer is null || !offer.IsCurrentlyValid(DateTime.UtcNow)
            ? null
            : await MapOfferAsync(offer, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ResolvedPriceDto> MapOfferAsync(
        Domain.Entities.ProductOffer offer,
        CancellationToken cancellationToken)
    {
        var availability = await MapAvailabilityAsync(offer.Id, offer.StoreId, cancellationToken).ConfigureAwait(false);
        return new ResolvedPriceDto(
            offer.Id,
            offer.ProductId,
            offer.VariantId,
            offer.StoreId,
            offer.CurrencyCode,
            offer.Price,
            offer.CompareAtPrice,
            DateTime.UtcNow,
            availability);
    }

    private async Task<StorefrontAvailabilityDto> MapAvailabilityAsync(
        int offerId,
        int storeId,
        CancellationToken cancellationToken)
    {
        var availabilityResult = await inventoryReader
            .GetStorefrontAvailabilityAsync(offerId, storeId, cancellationToken)
            .ConfigureAwait(false);

        if (!availabilityResult.IsSuccess || availabilityResult.Value is null)
        {
            return new StorefrontAvailabilityDto("NotTracked", true, false);
        }

        var availability = availabilityResult.Value;
        return new StorefrontAvailabilityDto(
            availability.AvailabilityStatus.ToString(),
            availability.CanPurchase,
            availability.IsBackorder);
    }
}
