namespace Commerce.Media.Contracts.Storage;

public sealed record MediaStorageRequest(
    string StorageKey,
    string ContentType,
    Stream Content,
    long? ContentLength = null);

public sealed record MediaStorageResult(
    bool Success,
    string StorageKey,
    long Size,
    string ContentType,
    string? Error = null);

public interface IMediaStorage
{
    Task<MediaStorageResult> SaveAsync(MediaStorageRequest request, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);
}
