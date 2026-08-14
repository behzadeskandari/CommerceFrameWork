using Commerce.Catalog.Contracts.Products;
using Commerce.Downloads.Application.Abstractions;
using Commerce.Downloads.Contracts.Admin;
using Commerce.Downloads.Domain.Entities;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Downloads.Application.Admin;

public sealed class DownloadAdminService(
    IDownloadRepository downloadRepository,
    IDownloadMediaResolver mediaResolver,
    IProductReader productReader,
    IStoreContext storeContext) : IDownloadAdminService
{
    public async Task<Result<ProductDownloadSettingsDto?>> GetSettingsAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var validation = await ValidateDigitalProductAsync(productId, cancellationToken).ConfigureAwait(false);
        if (validation.IsFailure)
        {
            return Result.Failure<ProductDownloadSettingsDto?>(validation.Error!);
        }

        var settings = await downloadRepository.GetSettingsAsync(productId, storeId, cancellationToken).ConfigureAwait(false);
        if (settings is null)
        {
            return Result.Success<ProductDownloadSettingsDto?>(null);
        }

        return Result.Success<ProductDownloadSettingsDto?>(MapSettings(settings));
    }

    public async Task<Result<ProductDownloadSettingsDto>> SaveSettingsAsync(
        int productId,
        SaveProductDownloadSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var storeId = RequireStoreId();
        var validation = await ValidateDigitalProductAsync(productId, cancellationToken).ConfigureAwait(false);
        if (validation.IsFailure)
        {
            return Result.Failure<ProductDownloadSettingsDto>(validation.Error!);
        }

        var existing = await downloadRepository.GetSettingsAsync(productId, storeId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            var created = ProductDownloadSettings.Create(
                productId,
                storeId,
                request.IsEnabled,
                request.MaxDownloadCount,
                request.ExpirationDays);
            await downloadRepository.AddSettingsAsync(created, cancellationToken).ConfigureAwait(false);
            await downloadRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Success(MapSettings(created));
        }

        existing.Update(request.IsEnabled, request.MaxDownloadCount, request.ExpirationDays);
        await downloadRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(MapSettings(existing));
    }

    public async Task<Result<IReadOnlyList<ProductDownloadFileDto>>> ListFilesAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var validation = await ValidateDigitalProductAsync(productId, cancellationToken).ConfigureAwait(false);
        if (validation.IsFailure)
        {
            return Result.Failure<IReadOnlyList<ProductDownloadFileDto>>(validation.Error!);
        }

        var files = await downloadRepository.ListFilesAsync(productId, storeId, cancellationToken).ConfigureAwait(false);
        var dtos = new List<ProductDownloadFileDto>();
        foreach (var file in files.OrderBy(x => x.DisplayOrder))
        {
            var mapped = await MapFileAsync(file, cancellationToken).ConfigureAwait(false);
            if (mapped is not null)
            {
                dtos.Add(mapped);
            }
        }

        return Result.Success<IReadOnlyList<ProductDownloadFileDto>>(dtos);
    }

    public async Task<Result<ProductDownloadFileDto>> AddFileAsync(
        int productId,
        AddProductDownloadFileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var storeId = RequireStoreId();
        var validation = await ValidateDigitalProductAsync(productId, cancellationToken).ConfigureAwait(false);
        if (validation.IsFailure)
        {
            return Result.Failure<ProductDownloadFileDto>(validation.Error!);
        }

        var media = await mediaResolver.ResolveAsync(request.MediaAssetId, storeId, cancellationToken).ConfigureAwait(false);
        if (media is null || media.IsDeleted)
        {
            return Result.Failure<ProductDownloadFileDto>(Error.NotFound("Media asset not found."));
        }

        var file = ProductDownloadFile.Create(
            productId,
            storeId,
            request.MediaAssetId,
            request.DisplayName,
            request.DisplayOrder,
            request.IsActive);

        await downloadRepository.AddFileAsync(file, cancellationToken).ConfigureAwait(false);
        await downloadRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await MapFileAsync(file, cancellationToken).ConfigureAwait(false);
        return dto is null
            ? Result.Failure<ProductDownloadFileDto>(Error.NotFound("Download file could not be resolved."))
            : Result.Success(dto);
    }

    public async Task<Result<ProductDownloadFileDto>> UpdateFileAsync(
        int productId,
        int fileId,
        UpdateProductDownloadFileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var storeId = RequireStoreId();
        var file = await downloadRepository.GetFileAsync(fileId, cancellationToken).ConfigureAwait(false);
        if (file is null || file.ProductId != productId || file.StoreId != storeId)
        {
            return Result.Failure<ProductDownloadFileDto>(Error.NotFound("Download file not found."));
        }

        file.Update(request.DisplayName, request.DisplayOrder, request.IsActive);
        await downloadRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await MapFileAsync(file, cancellationToken).ConfigureAwait(false);
        return dto is null
            ? Result.Failure<ProductDownloadFileDto>(Error.NotFound("Download file could not be resolved."))
            : Result.Success(dto);
    }

    public async Task<Result> RemoveFileAsync(int productId, int fileId, CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var file = await downloadRepository.GetFileAsync(fileId, cancellationToken).ConfigureAwait(false);
        if (file is null || file.ProductId != productId || file.StoreId != storeId)
        {
            return Result.Failure(Error.NotFound("Download file not found."));
        }

        await downloadRepository.RemoveFileAsync(file, cancellationToken).ConfigureAwait(false);
        await downloadRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<DownloadHistoryEntryDto>>> GetProductHistoryAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var entries = await downloadRepository.ListHistoryForProductAsync(productId, storeId, cancellationToken)
            .ConfigureAwait(false);

        var dtos = entries
            .OrderByDescending(x => x.DownloadedAtUtc)
            .Select(x => new DownloadHistoryEntryDto(
                x.Id,
                x.EntitlementId,
                x.ProductDownloadFileId,
                x.CustomerId,
                x.DownloadedAtUtc,
                x.WasSuccessful,
                x.FailureReason))
            .ToList();

        return Result.Success<IReadOnlyList<DownloadHistoryEntryDto>>(dtos);
    }

    private int RequireStoreId() =>
        storeContext.CurrentStoreId ?? throw new InvalidOperationException("Store context is required.");

    private async Task<Result> ValidateDigitalProductAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await productReader.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (!product.IsSuccess || product.Value is null)
        {
            return Result.Failure(Error.NotFound("Product not found."));
        }

        if (!DigitalProductTypes.IsDigital(product.Value.ProductType))
        {
            return Result.Failure(Error.Validation("Download settings are only available for digital product types."));
        }

        return Result.Success();
    }

    private static ProductDownloadSettingsDto MapSettings(ProductDownloadSettings settings) =>
        new(settings.ProductId, settings.IsEnabled, settings.MaxDownloadCount, settings.ExpirationDays);

    private async Task<ProductDownloadFileDto?> MapFileAsync(ProductDownloadFile file, CancellationToken cancellationToken)
    {
        var media = await mediaResolver.ResolveAsync(file.MediaAssetId, file.StoreId, cancellationToken).ConfigureAwait(false);
        if (media is null)
        {
            return null;
        }

        return new ProductDownloadFileDto(
            file.Id,
            file.ProductId,
            file.MediaAssetId,
            media.FileName,
            media.ContentType,
            media.Size,
            file.DisplayName,
            file.DisplayOrder,
            file.IsActive);
    }
}
