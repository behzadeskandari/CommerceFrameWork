using System.Security.Cryptography;
using Commerce.Framework.Contracts.Tenancy;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Media.Application.Abstractions;
using Commerce.Media.Contracts.Images;
using Commerce.Media.Contracts.Media;
using Commerce.Media.Contracts.Storage;
using Commerce.Media.Contracts.Urls;
using Commerce.Media.Domain.Entities;
using Commerce.Media.Domain.Enums;
using Commerce.Media.Domain.Services;

namespace Commerce.Media.Application;

public sealed class MediaService(
    IMediaAssetRepository repository,
    IMediaStorage storage,
    IImageProcessor imageProcessor,
    IMediaUrlResolver urlResolver,
    IStoreContext storeContext,
    MediaUploadValidator uploadValidator,
    MediaSettings settings) : IMediaService
{
    private const string LocalProvider = "local";

    public async Task<Result<IReadOnlyList<MediaSummaryDto>>> ListAsync(
        string? term = null,
        MediaType? mediaType = null,
        CancellationToken cancellationToken = default)
    {
        var storeId = RequireStoreId();
        var assets = await repository
            .ListAsync(storeId, term, mediaType, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<IReadOnlyList<MediaSummaryDto>>(
            assets.Where(x => !x.IsDeleted).Select(MapSummary).ToList());
    }

    public async Task<Result<MediaAssetDto>> GetByIdAsync(int mediaId, CancellationToken cancellationToken = default)
    {
        var asset = await repository.GetByIdAsync(mediaId, cancellationToken).ConfigureAwait(false);
        if (asset is null || asset.IsDeleted)
        {
            return Result.Failure<MediaAssetDto>(Error.NotFound($"Media '{mediaId}' was not found."));
        }

        if (!IsAuthorizedForStore(asset))
        {
            return Result.Failure<MediaAssetDto>(Error.NotFound($"Media '{mediaId}' was not found."));
        }

        return Result.Success(MapDetail(asset));
    }

    public async Task<IReadOnlyList<MediaSummaryDto>> GetByIdsAsync(
        IReadOnlyCollection<int> mediaIds,
        CancellationToken cancellationToken = default)
    {
        if (mediaIds.Count == 0)
        {
            return Array.Empty<MediaSummaryDto>();
        }

        var storeId = storeContext.CurrentStoreId;
        var assets = await repository.GetByIdsAsync(mediaIds, cancellationToken).ConfigureAwait(false);
        return assets
            .Where(x => !x.IsDeleted && (!storeId.HasValue || x.StoreId == storeId.Value))
            .Select(MapSummary)
            .ToList();
    }

    public async Task<Result<MediaAssetDto>> GetForStoreAsync(
        int mediaId,
        int storeId,
        CancellationToken cancellationToken = default)
    {
        var asset = await repository.GetByIdAsync(mediaId, cancellationToken).ConfigureAwait(false);
        if (asset is null || asset.IsDeleted || asset.StoreId != storeId)
        {
            return Result.Failure<MediaAssetDto>(Error.NotFound($"Media '{mediaId}' was not found."));
        }

        return Result.Success(MapDetail(asset));
    }

    public async Task<Result<MediaAssetDto>> UploadAsync(
        UploadMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var storeId = RequireStoreId();
        await using var buffered = new MemoryStream();
        await request.Content.CopyToAsync(buffered, cancellationToken).ConfigureAwait(false);
        var bytes = buffered.ToArray();

        if (bytes.Length != request.ContentLength)
        {
            return Result.Failure<MediaAssetDto>(Error.Validation("Uploaded content length mismatch."));
        }

        var header = bytes.AsMemory()[..Math.Min(bytes.Length, 32)];
        MediaType mediaType;
        string contentType;
        try
        {
            (mediaType, contentType) = await uploadValidator
                .ValidateAsync(header, request.ContentType, request.OriginalFileName, request.ContentLength, storeId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<MediaAssetDto>(Error.Validation(ex.Message));
        }

        var extension = FileNameSanitizer.GetSafeExtension(request.OriginalFileName, contentType);
        var storageKey = StorageKeyGenerator.Create(storeId, extension, DateTime.UtcNow);
        string? thumbnailKey = null;
        int? width = null;
        int? height = null;

        if (mediaType == MediaType.Image)
        {
            await using var imageStream = new MemoryStream(bytes);
            var dimensions = await imageProcessor.GetDimensionsAsync(imageStream, cancellationToken).ConfigureAwait(false);
            width = dimensions?.Width;
            height = dimensions?.Height;

            thumbnailKey = StorageKeyGenerator.CreateThumbnailKey(storageKey);
            await using var thumbSource = new MemoryStream(bytes);
            var thumbStream = await imageProcessor.GenerateThumbnailAsync(
                thumbSource,
                await settings.GetThumbnailMaxWidthAsync(storeId, cancellationToken).ConfigureAwait(false),
                await settings.GetThumbnailMaxHeightAsync(storeId, cancellationToken).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

            if (thumbStream is not null)
            {
                await using (thumbStream)
                {
                    var thumbResult = await storage.SaveAsync(
                        new MediaStorageRequest(thumbnailKey, contentType, thumbStream, thumbStream.Length),
                        cancellationToken).ConfigureAwait(false);

                    if (!thumbResult.Success)
                    {
                        thumbnailKey = null;
                    }
                }
            }
            else
            {
                thumbnailKey = null;
            }
        }

        await using var saveStream = new MemoryStream(bytes);
        var saveResult = await storage.SaveAsync(
            new MediaStorageRequest(storageKey, contentType, saveStream, bytes.Length),
            cancellationToken).ConfigureAwait(false);

        if (!saveResult.Success)
        {
            if (thumbnailKey is not null)
            {
                await storage.DeleteAsync(thumbnailKey, cancellationToken).ConfigureAwait(false);
            }

            return Result.Failure<MediaAssetDto>(Error.Validation(saveResult.Error ?? "Failed to save media file."));
        }

        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var asset = MediaAsset.Create(
            storeId,
            request.OriginalFileName,
            contentType,
            extension,
            bytes.Length,
            storageKey,
            LocalProvider,
            mediaType,
            hash,
            width,
            height,
            thumbnailKey,
            request.IsPublic);

        await repository.AddAsync(asset, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(asset));
    }

    public async Task<Result<MediaAssetDto>> UpdateAsync(
        int mediaId,
        UpdateMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var asset = await repository.GetByIdAsync(mediaId, cancellationToken).ConfigureAwait(false);
        if (asset is null || asset.IsDeleted)
        {
            return Result.Failure<MediaAssetDto>(Error.NotFound($"Media '{mediaId}' was not found."));
        }

        if (!IsAuthorizedForStore(asset))
        {
            return Result.Failure<MediaAssetDto>(Error.NotFound($"Media '{mediaId}' was not found."));
        }

        asset.UpdateMetadata(request.Title, request.AltText, request.IsPublic);
        await repository.UpdateAsync(asset, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(asset));
    }

    public async Task<Result> DeleteAsync(int mediaId, CancellationToken cancellationToken = default)
    {
        var asset = await repository.GetByIdAsync(mediaId, cancellationToken).ConfigureAwait(false);
        if (asset is null || asset.IsDeleted)
        {
            return Result.Failure(Error.NotFound($"Media '{mediaId}' was not found."));
        }

        if (!IsAuthorizedForStore(asset))
        {
            return Result.Failure(Error.NotFound($"Media '{mediaId}' was not found."));
        }

        asset.SoftDelete();
        await repository.UpdateAsync(asset, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<Stream>> OpenPublicReadAsync(
        int mediaId,
        bool thumbnail = false,
        CancellationToken cancellationToken = default)
    {
        var asset = await repository.GetByIdAsync(mediaId, cancellationToken).ConfigureAwait(false);
        if (asset is null || !asset.IsAccessibleAnonymously())
        {
            return Result.Failure<Stream>(Error.NotFound($"Media '{mediaId}' was not found."));
        }

        var storeId = storeContext.CurrentStoreId;
        if (storeId.HasValue && asset.StoreId != storeId.Value)
        {
            return Result.Failure<Stream>(Error.NotFound($"Media '{mediaId}' was not found."));
        }

        return await OpenStreamAsync(asset, thumbnail, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<Stream>> OpenAuthorizedReadAsync(
        int mediaId,
        bool thumbnail = false,
        CancellationToken cancellationToken = default)
    {
        var asset = await repository.GetByIdAsync(mediaId, cancellationToken).ConfigureAwait(false);
        if (asset is null || asset.IsDeleted)
        {
            return Result.Failure<Stream>(Error.NotFound($"Media '{mediaId}' was not found."));
        }

        if (!IsAuthorizedForStore(asset))
        {
            return Result.Failure<Stream>(Error.NotFound($"Media '{mediaId}' was not found."));
        }

        return await OpenStreamAsync(asset, thumbnail, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<Stream>> OpenStreamAsync(
        MediaAsset asset,
        bool thumbnail,
        CancellationToken cancellationToken)
    {
        var key = thumbnail ? asset.ThumbnailStorageKey ?? asset.StorageKey : asset.StorageKey;
        var stream = await storage.OpenReadAsync(key, cancellationToken).ConfigureAwait(false);
        return stream is null
            ? Result.Failure<Stream>(Error.NotFound($"Media file '{asset.Id}' was not found."))
            : Result.Success(stream);
    }

    private int RequireStoreId()
    {
        if (!storeContext.CurrentStoreId.HasValue)
        {
            throw new InvalidOperationException("Store context is required for media operations.");
        }

        return storeContext.CurrentStoreId.Value;
    }

    private bool IsAuthorizedForStore(MediaAsset asset)
    {
        var storeId = storeContext.CurrentStoreId;
        return !storeId.HasValue || asset.StoreId == storeId.Value;
    }

    private MediaSummaryDto MapSummary(MediaAsset asset) =>
        new(
            asset.Id,
            asset.StoreId,
            asset.FileName,
            asset.OriginalFileName,
            asset.ContentType,
            asset.Extension,
            asset.Size,
            asset.MediaType.ToString(),
            asset.IsPublic,
            asset.Width,
            asset.Height,
            asset.Title,
            asset.AltText,
            urlResolver.GetMediaUrl(asset.Id),
            urlResolver.GetThumbnailUrl(asset.Id, asset.ThumbnailStorageKey),
            asset.CreatedAtUtc);

    private MediaAssetDto MapDetail(MediaAsset asset) =>
        new(
            asset.Id,
            asset.StoreId,
            asset.FileName,
            asset.OriginalFileName,
            asset.ContentType,
            asset.Extension,
            asset.Size,
            asset.MediaType.ToString(),
            asset.IsPublic,
            asset.IsDeleted,
            asset.Width,
            asset.Height,
            asset.Title,
            asset.AltText,
            asset.ContentHash,
            urlResolver.GetMediaUrl(asset.Id),
            urlResolver.GetThumbnailUrl(asset.Id, asset.ThumbnailStorageKey),
            asset.CreatedAtUtc,
            asset.UpdatedAtUtc);
}
