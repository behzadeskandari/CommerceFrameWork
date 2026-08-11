using Commerce.Catalog.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Catalog.Domain.Entities;

public sealed class ProductVariantMedia : Entity
{
    private ProductVariantMedia()
    {
    }

    public int VariantId { get; private set; }

    public int MediaAssetId { get; private set; }

    public ProductMediaRole Role { get; private set; }

    public int DisplayOrder { get; private set; }

    public static ProductVariantMedia Create(int variantId, int mediaAssetId, ProductMediaRole role, int displayOrder = 0)
    {
        if (variantId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(variantId));
        }

        if (mediaAssetId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaAssetId));
        }

        return new ProductVariantMedia
        {
            VariantId = variantId,
            MediaAssetId = mediaAssetId,
            Role = role,
            DisplayOrder = displayOrder
        };
    }
}
