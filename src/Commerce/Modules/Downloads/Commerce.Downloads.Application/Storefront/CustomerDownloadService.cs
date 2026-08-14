using Commerce.Downloads.Application.Abstractions;
using Commerce.Downloads.Contracts.Storage;
using Commerce.Downloads.Contracts.Storefront;
using Commerce.Downloads.Domain.Entities;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Orders.Contracts.Orders;
using Commerce.Orders.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Commerce.Downloads.Application.Storefront;

public sealed class CustomerDownloadService(
    IDownloadRepository downloadRepository,
    IDownloadMediaResolver mediaResolver,
    IDownloadStorage downloadStorage,
    IOrderPaymentSyncRepository orderRepository,
    IStoreContext storeContext,
    ILogger<CustomerDownloadService> logger) : ICustomerDownloadService
{
    public async Task<Result<IReadOnlyList<CustomerDownloadEntitlementDto>>> ListAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var entitlements = await downloadRepository
            .ListEntitlementsForCustomerAsync(customerId, storeId, cancellationToken)
            .ConfigureAwait(false);

        var dtos = new List<CustomerDownloadEntitlementDto>();
        foreach (var entitlement in entitlements.OrderByDescending(x => x.GrantedAtUtc))
        {
            var order = await orderRepository.GetByIdAsync(entitlement.OrderId, cancellationToken).ConfigureAwait(false);
            if (order is null || order.PaymentStatus != PaymentStatus.Paid)
            {
                continue;
            }

            var item = order.Items.FirstOrDefault(x => x.Id == entitlement.OrderItemId);
            var files = await BuildFileDtosAsync(entitlement.ProductId, storeId, cancellationToken).ConfigureAwait(false);
            if (files.Count == 0)
            {
                continue;
            }

            dtos.Add(new CustomerDownloadEntitlementDto(
                entitlement.Id,
                entitlement.OrderId,
                order.OrderNumber,
                entitlement.ProductId,
                item?.ProductName ?? $"Product #{entitlement.ProductId}",
                entitlement.GrantedAtUtc,
                entitlement.ExpiresAtUtc,
                entitlement.MaxDownloadCount,
                entitlement.DownloadCount,
                CalculateRemaining(entitlement),
                files));
        }

        return Result.Success<IReadOnlyList<CustomerDownloadEntitlementDto>>(dtos);
    }

    public async Task<Result<DownloadFileContent>> DownloadAsync(
        int customerId,
        int entitlementId,
        int fileId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var utcNow = DateTime.UtcNow;

        var entitlement = await downloadRepository.GetEntitlementAsync(entitlementId, cancellationToken).ConfigureAwait(false);
        if (entitlement is null || entitlement.StoreId != storeId || !entitlement.IsOwnedByCustomer(customerId))
        {
            await RecordFailureAsync(null, fileId, customerId, utcNow, ipAddress, userAgent, "Unauthorized.", cancellationToken)
                .ConfigureAwait(false);
            return Result.Failure<DownloadFileContent>(Error.Forbidden("Download is not authorized."));
        }

        var order = await orderRepository.GetByIdAsync(entitlement.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null || order.PaymentStatus != PaymentStatus.Paid)
        {
            await RecordFailureAsync(entitlement, fileId, customerId, utcNow, ipAddress, userAgent, "Order not paid.", cancellationToken)
                .ConfigureAwait(false);
            return Result.Failure<DownloadFileContent>(Error.Forbidden("Download is not authorized."));
        }

        var file = await downloadRepository.GetFileAsync(fileId, cancellationToken).ConfigureAwait(false);
        if (file is null || file.ProductId != entitlement.ProductId || !file.IsActive)
        {
            await RecordFailureAsync(entitlement, fileId, customerId, utcNow, ipAddress, userAgent, "File not found.", cancellationToken)
                .ConfigureAwait(false);
            return Result.Failure<DownloadFileContent>(Error.NotFound("Download file not found."));
        }

        if (entitlement.IsRevoked || entitlement.IsExpired(utcNow) || !entitlement.HasRemainingDownloads())
        {
            var reason = entitlement.IsRevoked ? "Revoked." : entitlement.IsExpired(utcNow) ? "Expired." : "Limit exceeded.";
            await RecordFailureAsync(entitlement, fileId, customerId, utcNow, ipAddress, userAgent, reason, cancellationToken)
                .ConfigureAwait(false);
            return Result.Failure<DownloadFileContent>(Error.Forbidden("Download is not authorized."));
        }

        var media = await mediaResolver.ResolveAsync(file.MediaAssetId, storeId, cancellationToken).ConfigureAwait(false);
        if (media is null || media.IsDeleted || !IsValidStorageKey(media.StorageKey))
        {
            await RecordFailureAsync(entitlement, fileId, customerId, utcNow, ipAddress, userAgent, "Storage unavailable.", cancellationToken)
                .ConfigureAwait(false);
            return Result.Failure<DownloadFileContent>(Error.NotFound("Download file not found."));
        }

        var stream = await downloadStorage.OpenReadAsync(media.StorageKey, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            await RecordFailureAsync(entitlement, fileId, customerId, utcNow, ipAddress, userAgent, "File missing.", cancellationToken)
                .ConfigureAwait(false);
            return Result.Failure<DownloadFileContent>(Error.NotFound("Download file not found."));
        }

        try
        {
            entitlement.RecordSuccessfulDownload(utcNow);
            await downloadRepository.AddHistoryAsync(
                DownloadHistoryEntry.Record(entitlement.Id, file.Id, customerId, utcNow, true, ipAddress, userAgent),
                cancellationToken).ConfigureAwait(false);
            await downloadRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            await RecordFailureAsync(entitlement, fileId, customerId, utcNow, ipAddress, userAgent, ex.Message, cancellationToken)
                .ConfigureAwait(false);
            return Result.Failure<DownloadFileContent>(Error.Forbidden(ex.Message));
        }

        var fileName = file.DisplayName ?? media.FileName;
        logger.LogInformation(
            "Customer {CustomerId} downloaded file {FileId} for entitlement {EntitlementId}.",
            customerId,
            fileId,
            entitlementId);

        return Result.Success(new DownloadFileContent(stream, fileName, media.ContentType, media.Size));
    }

    private async Task<IReadOnlyList<CustomerDownloadFileDto>> BuildFileDtosAsync(
        int productId,
        int storeId,
        CancellationToken cancellationToken)
    {
        var files = await downloadRepository.ListFilesAsync(productId, storeId, cancellationToken).ConfigureAwait(false);
        var dtos = new List<CustomerDownloadFileDto>();

        foreach (var file in files.Where(x => x.IsActive).OrderBy(x => x.DisplayOrder))
        {
            var media = await mediaResolver.ResolveAsync(file.MediaAssetId, storeId, cancellationToken).ConfigureAwait(false);
            if (media is null || media.IsDeleted)
            {
                continue;
            }

            dtos.Add(new CustomerDownloadFileDto(
                file.Id,
                file.DisplayName ?? media.FileName,
                media.ContentType,
                media.Size,
                file.DisplayName));
        }

        return dtos;
    }

    private static int? CalculateRemaining(DownloadEntitlement entitlement) =>
        entitlement.MaxDownloadCount.HasValue
            ? Math.Max(0, entitlement.MaxDownloadCount.Value - entitlement.DownloadCount)
            : null;

    private int RequireStoreId() =>
        storeContext.CurrentStoreId ?? throw new InvalidOperationException("Store context is required.");

    private async Task RecordFailureAsync(
        DownloadEntitlement? entitlement,
        int fileId,
        int? customerId,
        DateTime utcNow,
        string? ipAddress,
        string? userAgent,
        string reason,
        CancellationToken cancellationToken)
    {
        if (entitlement is null)
        {
            return;
        }

        await downloadRepository.AddHistoryAsync(
            DownloadHistoryEntry.Record(entitlement.Id, fileId, customerId, utcNow, false, ipAddress, userAgent, reason),
            cancellationToken).ConfigureAwait(false);
        await downloadRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public static bool IsValidStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return false;
        }

        if (Path.IsPathRooted(storageKey))
        {
            return false;
        }

        var normalized = storageKey.Replace('\\', '/');
        return !normalized.Contains("../", StringComparison.Ordinal) &&
               !normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(x => x is "." or "..");
    }
}
