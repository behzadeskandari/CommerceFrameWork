using Commerce.Media.Contracts.Storage;
using Commerce.Media.Domain.Services;
using Microsoft.Extensions.Options;

namespace Commerce.Media.Infrastructure.Storage;

public sealed class LocalMediaStorage(IOptions<MediaStorageOptions> options) : IMediaStorage
{
    public async Task<MediaStorageResult> SaveAsync(MediaStorageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        StorageKeyGenerator.ValidateStorageKey(request.StorageKey);

        var physicalPath = GetPhysicalPath(request.StorageKey);
        var directory = Path.GetDirectoryName(physicalPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var file = new FileStream(
            physicalPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await request.Content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
        var size = file.Length;

        return new MediaStorageResult(true, request.StorageKey, size, request.ContentType);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        StorageKeyGenerator.ValidateStorageKey(storageKey);
        var physicalPath = GetPhysicalPath(storageKey);
        if (!File.Exists(physicalPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            physicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        StorageKeyGenerator.ValidateStorageKey(storageKey);
        var physicalPath = GetPhysicalPath(storageKey);
        if (!File.Exists(physicalPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(physicalPath);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        StorageKeyGenerator.ValidateStorageKey(storageKey);
        return Task.FromResult(File.Exists(GetPhysicalPath(storageKey)));
    }

    private string GetPhysicalPath(string storageKey)
    {
        var root = Path.GetFullPath(options.Value.StorageRoot);
        var combined = Path.GetFullPath(Path.Combine(root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved media path is outside the storage root.");
        }

        return combined;
    }
}
