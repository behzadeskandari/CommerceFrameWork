using Commerce.Catalog.Contracts.Media;
using Commerce.Checkout.Contracts.Checkout;

namespace Commerce.Checkout.Application.Checkout;

public interface ICheckoutItemEnricher
{
    Task<IReadOnlyDictionary<int, CheckoutItemImageDto>> GetImagesByOfferAsync(
        IReadOnlyCollection<(int OfferId, int ProductId, int? VariantId)> items,
        CancellationToken cancellationToken = default);
}

public sealed class CheckoutItemEnricher(IProductMediaReader productMediaReader) : ICheckoutItemEnricher
{
    public async Task<IReadOnlyDictionary<int, CheckoutItemImageDto>> GetImagesByOfferAsync(
        IReadOnlyCollection<(int OfferId, int ProductId, int? VariantId)> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return new Dictionary<int, CheckoutItemImageDto>();
        }

        var productIds = items.Select(x => x.ProductId).Distinct().ToList();
        var primaryByProduct = await productMediaReader
            .GetPrimaryForProductsAsync(productIds, cancellationToken)
            .ConfigureAwait(false);

        var result = new Dictionary<int, CheckoutItemImageDto>();
        foreach (var (offerId, productId, variantId) in items)
        {
            ProductMediaSummaryDto? media = null;
            if (variantId.HasValue)
            {
                var variantMedia = await productMediaReader
                    .GetForVariantAsync(variantId.Value, cancellationToken)
                    .ConfigureAwait(false);
                media = variantMedia.FirstOrDefault();
            }

            media ??= primaryByProduct.GetValueOrDefault(productId);
            if (media is null)
            {
                continue;
            }

            result[offerId] = new CheckoutItemImageDto(media.Url, media.ThumbnailUrl, media.AltText);
        }

        return result;
    }
}
