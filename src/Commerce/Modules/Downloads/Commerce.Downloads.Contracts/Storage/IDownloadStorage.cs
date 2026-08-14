namespace Commerce.Downloads.Contracts.Storage;

public interface IDownloadStorage
{
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed record DownloadFileContent(
    Stream Content,
    string FileName,
    string ContentType,
    long Size);
