using Commerce.Framework.Core.Results;
using Commerce.Catalog.Contracts.Products;

namespace Commerce.Catalog.Contracts.Pricing;

public sealed record ResolvedPriceDto(
    int OfferId,
    int ProductId,
    int? VariantId,
    int StoreId,
    string CurrencyCode,
    decimal UnitPrice,
    decimal? CompareAtPrice,
    DateTime ResolvedAtUtc,
    StorefrontAvailabilityDto? Availability = null);

public interface IPricingService
{
    Task<Result<ResolvedPriceDto>> ResolveProductPriceAsync(
        int productId,
        int? currencyId = null,
        CancellationToken cancellationToken = default);

    Task<Result<ResolvedPriceDto>> ResolveVariantPriceAsync(
        int variantId,
        int? currencyId = null,
        CancellationToken cancellationToken = default);
}

public interface ICatalogPricingReader
{
    Task<ResolvedPriceDto?> GetOfferPriceAsync(int offerId, CancellationToken cancellationToken = default);
}
