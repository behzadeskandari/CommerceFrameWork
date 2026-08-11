using Commerce.Media.Contracts.Urls;

namespace Commerce.Media.Infrastructure.Urls;

public sealed class MediaUrlResolver : IMediaUrlResolver
{
    public string GetMediaUrl(int mediaId) => $"/api/media/{mediaId}";

    public string? GetThumbnailUrl(int mediaId, string? thumbnailStorageKey) =>
        thumbnailStorageKey is null ? null : $"/api/media/{mediaId}/thumbnail";
}
