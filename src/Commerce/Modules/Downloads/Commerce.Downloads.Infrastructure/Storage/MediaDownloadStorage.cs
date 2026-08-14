using Commerce.Downloads.Contracts.Storage;
using Commerce.Media.Contracts.Storage;

namespace Commerce.Downloads.Infrastructure.Storage;

public sealed class MediaDownloadStorage(IMediaStorage mediaStorage) : IDownloadStorage
{
    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
        mediaStorage.OpenReadAsync(storageKey, cancellationToken);

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default) =>
        mediaStorage.ExistsAsync(storageKey, cancellationToken);
}
