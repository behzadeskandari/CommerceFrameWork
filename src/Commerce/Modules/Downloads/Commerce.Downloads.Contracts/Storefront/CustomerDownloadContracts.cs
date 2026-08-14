using Commerce.Downloads.Contracts.Storage;
using Commerce.Framework.Core.Results;

namespace Commerce.Downloads.Contracts.Storefront;

public sealed record CustomerDownloadFileDto(
    int FileId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string? DisplayName);

public sealed record CustomerDownloadEntitlementDto(
    int EntitlementId,
    int OrderId,
    string OrderNumber,
    int ProductId,
    string ProductName,
    DateTime GrantedAtUtc,
    DateTime? ExpiresAtUtc,
    int? MaxDownloadCount,
    int DownloadCount,
    int? RemainingDownloads,
    IReadOnlyList<CustomerDownloadFileDto> Files);

public interface ICustomerDownloadService
{
    Task<Result<IReadOnlyList<CustomerDownloadEntitlementDto>>> ListAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<Result<DownloadFileContent>> DownloadAsync(
        int customerId,
        int entitlementId,
        int fileId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
