using Commerce.Framework.Core.Entities;

namespace Commerce.Catalog.Domain.Entities;

public sealed class CategoryMedia : Entity
{
    private CategoryMedia()
    {
    }

    public int CategoryId { get; private set; }

    public int MediaAssetId { get; private set; }

    public int DisplayOrder { get; private set; }

    public static CategoryMedia Create(int categoryId, int mediaAssetId, int displayOrder = 0)
    {
        if (categoryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categoryId));
        }

        if (mediaAssetId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaAssetId));
        }

        return new CategoryMedia
        {
            CategoryId = categoryId,
            MediaAssetId = mediaAssetId,
            DisplayOrder = displayOrder
        };
    }
}
