using Commerce.Framework.Core.Entities;
using Commerce.Store.Domain.Enums;

namespace Commerce.Store.Domain.Entities;

public sealed class StoreMedia : Entity
{
    private StoreMedia()
    {
    }

    public int StoreId { get; private set; }

    public int MediaAssetId { get; private set; }

    public StoreMediaRole Role { get; private set; }

    public static StoreMedia Create(int storeId, int mediaAssetId, StoreMediaRole role)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        if (mediaAssetId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaAssetId));
        }

        return new StoreMedia
        {
            StoreId = storeId,
            MediaAssetId = mediaAssetId,
            Role = role
        };
    }
}
