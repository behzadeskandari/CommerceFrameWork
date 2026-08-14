using Commerce.Downloads.Application.Abstractions;
using Commerce.Media.Application.Abstractions;

namespace Commerce.Downloads.Infrastructure.Media;

public sealed class DownloadMediaResolver(IMediaAssetRepository mediaAssetRepository) : IDownloadMediaResolver
{
    public async Task<ResolvedDownloadMedia?> ResolveAsync(
        int mediaAssetId,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var asset = await mediaAssetRepository.GetByIdAsync(mediaAssetId, cancellationToken).ConfigureAwait(false);
        if (asset is null || asset.StoreId != storeId)
        {
            return null;
        }

        return new ResolvedDownloadMedia(
            asset.Id,
            asset.StorageKey,
            asset.FileName,
            asset.ContentType,
            asset.Size,
            asset.IsDeleted);
    }
}
