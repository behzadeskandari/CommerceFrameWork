using Commerce.Cart.Application.Abstractions;
using Commerce.Catalog.Contracts.Media;

namespace Commerce.Cart.Application.Carts;

public sealed class CartItemDisplayEnricher(IProductMediaReader productMediaReader) : ICartItemDisplayEnricher
{
    public async Task<IReadOnlyDictionary<int, CartItemImageInfo>> GetPrimaryImagesByOfferAsync(
        IReadOnlyCollection<(int OfferId, int ProductId, int? VariantId)> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return new Dictionary<int, CartItemImageInfo>();
        }

        var productIds = items.Select(x => x.ProductId).Distinct().ToList();
        var primaryByProduct = await productMediaReader
            .GetPrimaryForProductsAsync(productIds, cancellationToken)
            .ConfigureAwait(false);

        var result = new Dictionary<int, CartItemImageInfo>();
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

            result[offerId] = new CartItemImageInfo(media.Url, media.ThumbnailUrl, media.AltText);
        }

        return result;
    }
}
