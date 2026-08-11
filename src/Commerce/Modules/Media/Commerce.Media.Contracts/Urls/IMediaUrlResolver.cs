namespace Commerce.Media.Contracts.Urls;

public interface IMediaUrlResolver
{
    string GetMediaUrl(int mediaId);

    string? GetThumbnailUrl(int mediaId, string? thumbnailStorageKey);
}
