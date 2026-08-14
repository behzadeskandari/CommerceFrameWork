using Commerce.Framework.Core.Results;

namespace Commerce.Downloads.Contracts.Admin;

public sealed record ProductDownloadSettingsDto(
    int ProductId,
    bool IsEnabled,
    int? MaxDownloadCount,
    int? ExpirationDays);

public sealed record SaveProductDownloadSettingsRequest(
    bool IsEnabled,
    int? MaxDownloadCount,
    int? ExpirationDays);

public sealed record ProductDownloadFileDto(
    int Id,
    int ProductId,
    int MediaAssetId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string? DisplayName,
    int DisplayOrder,
    bool IsActive);

public sealed record AddProductDownloadFileRequest(
    int MediaAssetId,
    string? DisplayName,
    int DisplayOrder,
    bool IsActive = true);

public sealed record UpdateProductDownloadFileRequest(
    string? DisplayName,
    int DisplayOrder,
    bool IsActive);

public sealed record DownloadHistoryEntryDto(
    int Id,
    int EntitlementId,
    int ProductDownloadFileId,
    int? CustomerId,
    DateTime DownloadedAtUtc,
    bool WasSuccessful,
    string? FailureReason);

public interface IDownloadAdminService
{
    Task<Result<ProductDownloadSettingsDto?>> GetSettingsAsync(int productId, CancellationToken cancellationToken = default);

    Task<Result<ProductDownloadSettingsDto>> SaveSettingsAsync(
        int productId,
        SaveProductDownloadSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ProductDownloadFileDto>>> ListFilesAsync(int productId, CancellationToken cancellationToken = default);

    Task<Result<ProductDownloadFileDto>> AddFileAsync(
        int productId,
        AddProductDownloadFileRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ProductDownloadFileDto>> UpdateFileAsync(
        int productId,
        int fileId,
        UpdateProductDownloadFileRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveFileAsync(int productId, int fileId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<DownloadHistoryEntryDto>>> GetProductHistoryAsync(
        int productId,
        CancellationToken cancellationToken = default);
}
