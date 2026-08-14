using Commerce.Downloads.Domain.Entities;

namespace Commerce.Downloads.Application.Abstractions;

public interface IDownloadRepository
{
    Task<ProductDownloadSettings?> GetSettingsAsync(int productId, int storeId, CancellationToken cancellationToken = default);

    Task AddSettingsAsync(ProductDownloadSettings settings, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDownloadFile>> ListFilesAsync(int productId, int storeId, CancellationToken cancellationToken = default);

    Task<ProductDownloadFile?> GetFileAsync(int fileId, CancellationToken cancellationToken = default);

    Task AddFileAsync(ProductDownloadFile file, CancellationToken cancellationToken = default);

    Task RemoveFileAsync(ProductDownloadFile file, CancellationToken cancellationToken = default);

    Task<bool> EntitlementExistsForOrderItemAsync(int orderItemId, CancellationToken cancellationToken = default);

    Task AddEntitlementAsync(DownloadEntitlement entitlement, CancellationToken cancellationToken = default);

    Task<DownloadEntitlement?> GetEntitlementAsync(int entitlementId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DownloadEntitlement>> ListEntitlementsForCustomerAsync(
        int customerId,
        int storeId,
        CancellationToken cancellationToken = default);

    Task AddHistoryAsync(DownloadHistoryEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DownloadHistoryEntry>> ListHistoryForProductAsync(
        int productId,
        int storeId,
        CancellationToken cancellationToken = default);
}

public interface IDownloadEntitlementService
{
    Task GrantForPaidOrderAsync(int orderId, CancellationToken cancellationToken = default);
}

public interface IDownloadMediaResolver
{
    Task<ResolvedDownloadMedia?> ResolveAsync(int mediaAssetId, int storeId, CancellationToken cancellationToken = default);
}

public sealed record ResolvedDownloadMedia(
    int MediaAssetId,
    string StorageKey,
    string FileName,
    string ContentType,
    long Size,
    bool IsDeleted);
