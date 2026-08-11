using Commerce.Framework.Core.Entities;
using Commerce.Media.Domain.Enums;
using Commerce.Media.Domain.Services;

namespace Commerce.Media.Domain.Entities;

public sealed class MediaAsset : AggregateRoot
{
    public const int FileNameMaxLength = 255;
    public const int ContentTypeMaxLength = 128;
    public const int ExtensionMaxLength = 16;
    public const int StorageKeyMaxLength = 512;
    public const int StorageProviderMaxLength = 64;
    public const int AltTextMaxLength = 500;
    public const int TitleMaxLength = 400;
    public const int ContentHashMaxLength = 128;

    private MediaAsset()
    {
    }

    public int StoreId { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public string Extension { get; private set; } = string.Empty;

    public long Size { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public string StorageProvider { get; private set; } = string.Empty;

    public string? ThumbnailStorageKey { get; private set; }

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    public string? AltText { get; private set; }

    public string? Title { get; private set; }

    public string? ContentHash { get; private set; }

    public MediaType MediaType { get; private set; }

    public bool IsPublic { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static MediaAsset Create(
        int storeId,
        string originalFileName,
        string contentType,
        string extension,
        long size,
        string storageKey,
        string storageProvider,
        MediaType mediaType,
        string? contentHash = null,
        int? width = null,
        int? height = null,
        string? thumbnailStorageKey = null,
        bool isPublic = true)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageProvider);

        StorageKeyGenerator.ValidateStorageKey(storageKey);
        if (thumbnailStorageKey is not null)
        {
            StorageKeyGenerator.ValidateStorageKey(thumbnailStorageKey);
        }

        var sanitizedOriginal = FileNameSanitizer.SanitizeOriginalFileName(originalFileName);
        var safeExtension = FileNameSanitizer.GetSafeExtension(sanitizedOriginal, contentType);
        var normalizedExtension = string.IsNullOrWhiteSpace(extension) ? safeExtension : extension.Trim().TrimStart('.').ToLowerInvariant();
        var now = DateTime.UtcNow;

        return new MediaAsset
        {
            StoreId = storeId,
            FileName = $"{GetFileNameWithoutExtension(sanitizedOriginal)}.{normalizedExtension}",
            OriginalFileName = sanitizedOriginal,
            ContentType = contentType.Trim(),
            Extension = normalizedExtension,
            Size = size,
            StorageKey = storageKey,
            StorageProvider = storageProvider.Trim(),
            ThumbnailStorageKey = thumbnailStorageKey,
            Width = width,
            Height = height,
            ContentHash = contentHash?.Trim(),
            MediaType = mediaType,
            IsPublic = isPublic,
            IsDeleted = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void UpdateMetadata(string? title, string? altText, bool isPublic)
    {
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim()[..Math.Min(title.Trim().Length, TitleMaxLength)];
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim()[..Math.Min(altText.Trim().Length, AltTextMaxLength)];
        IsPublic = isPublic;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool CanBeAccessedByStore(int storeId) => StoreId == storeId;

    public bool IsAccessibleAnonymously() => IsPublic && !IsDeleted;

    private static string GetFileNameWithoutExtension(string fileName)
    {
        var dot = fileName.LastIndexOf('.');
        return dot >= 0 ? fileName[..dot] : fileName;
    }
}
