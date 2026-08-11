using Commerce.Catalog.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Catalog.Domain.Entities;

public sealed class ProductMedia : Entity
{
    private ProductMedia()
    {
    }

    public int ProductId { get; private set; }

    public int MediaAssetId { get; private set; }

    public ProductMediaRole Role { get; private set; }

    public int DisplayOrder { get; private set; }

    public static ProductMedia Create(int productId, int mediaAssetId, ProductMediaRole role, int displayOrder = 0)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        if (mediaAssetId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaAssetId));
        }

        return new ProductMedia
        {
            ProductId = productId,
            MediaAssetId = mediaAssetId,
            Role = role,
            DisplayOrder = displayOrder
        };
    }

    public void Update(int displayOrder, ProductMediaRole role)
    {
        DisplayOrder = displayOrder;
        Role = role;
    }
}
