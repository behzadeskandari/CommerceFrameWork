using Commerce.Framework.Contracts.Configuration;
using Commerce.Media.Domain.Enums;

namespace Commerce.Media.Application;

public static class MediaSettingKeys
{
    public const string MaxUploadSize = "Media.MaxUploadSize";
    public const string MaxImageSize = "Media.MaxImageSize";
    public const string AllowedImageTypes = "Media.AllowedImageTypes";
    public const string AllowedDocumentTypes = "Media.AllowedDocumentTypes";
    public const string ThumbnailMaxWidth = "Media.ThumbnailMaxWidth";
    public const string ThumbnailMaxHeight = "Media.ThumbnailMaxHeight";
}

public sealed class MediaSettings(ISettingService settingService)
{
    public async Task<long> GetMaxUploadSizeAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<long>(MediaSettingKeys.MaxUploadSize, storeId, cancellationToken).ConfigureAwait(false);
        return value > 0 ? value : 10L * 1024 * 1024;
    }

    public async Task<long> GetMaxImageSizeAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<long>(MediaSettingKeys.MaxImageSize, storeId, cancellationToken).ConfigureAwait(false);
        return value > 0 ? value : 5L * 1024 * 1024;
    }

    public async Task<IReadOnlySet<string>> GetAllowedImageTypesAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var raw = await settingService.GetRawAsync(MediaSettingKeys.AllowedImageTypes, storeId, cancellationToken).ConfigureAwait(false)
            ?? "jpg,jpeg,png,gif,webp";
        return ParseList(raw);
    }

    public async Task<IReadOnlySet<string>> GetAllowedDocumentTypesAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var raw = await settingService.GetRawAsync(MediaSettingKeys.AllowedDocumentTypes, storeId, cancellationToken).ConfigureAwait(false)
            ?? "pdf";
        return ParseList(raw);
    }

    public async Task<int> GetThumbnailMaxWidthAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<int>(MediaSettingKeys.ThumbnailMaxWidth, storeId, cancellationToken).ConfigureAwait(false);
        return value > 0 ? value : 320;
    }

    public async Task<int> GetThumbnailMaxHeightAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var value = await settingService.GetAsync<int>(MediaSettingKeys.ThumbnailMaxHeight, storeId, cancellationToken).ConfigureAwait(false);
        return value > 0 ? value : 320;
    }

    private static IReadOnlySet<string> ParseList(string raw) =>
        raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

public sealed class MediaUploadValidator(MediaSettings settings)
{
    public async Task<(MediaType MediaType, string ContentType)> ValidateAsync(
        ReadOnlyMemory<byte> header,
        string? declaredContentType,
        string originalFileName,
        long contentLength,
        int? storeId,
        CancellationToken cancellationToken = default)
    {
        if (contentLength <= 0)
        {
            throw new ArgumentException("Uploaded file is empty.");
        }

        var maxUpload = await settings.GetMaxUploadSizeAsync(storeId, cancellationToken).ConfigureAwait(false);
        if (contentLength > maxUpload)
        {
            throw new ArgumentException($"File exceeds maximum upload size of {maxUpload} bytes.");
        }

        if (Domain.Services.FileSignatureValidator.IsExecutable(header.Span))
        {
            throw new ArgumentException("Executable files are not allowed.");
        }

        var extension = Domain.Services.FileNameSanitizer.GetSafeExtension(originalFileName, declaredContentType);
        var allowedImages = await settings.GetAllowedImageTypesAsync(storeId, cancellationToken).ConfigureAwait(false);
        var allowedDocuments = await settings.GetAllowedDocumentTypesAsync(storeId, cancellationToken).ConfigureAwait(false);

        if (Domain.Services.FileSignatureValidator.IsSupportedImage(header.Span))
        {
            var maxImage = await settings.GetMaxImageSizeAsync(storeId, cancellationToken).ConfigureAwait(false);
            if (contentLength > maxImage)
            {
                throw new ArgumentException($"Image exceeds maximum size of {maxImage} bytes.");
            }

            if (!allowedImages.Contains(extension))
            {
                throw new ArgumentException($"Image type '{extension}' is not allowed.");
            }

            var detected = Domain.Services.FileSignatureValidator.DetectImageContentType(header.Span)
                ?? declaredContentType
                ?? "application/octet-stream";
            return (MediaType.Image, detected);
        }

        if (Domain.Services.FileSignatureValidator.IsSupportedDocument(header.Span))
        {
            if (!allowedDocuments.Contains(extension))
            {
                throw new ArgumentException($"Document type '{extension}' is not allowed.");
            }

            return (MediaType.Document, "application/pdf");
        }

        throw new ArgumentException("Unsupported or invalid file type.");
    }
}
