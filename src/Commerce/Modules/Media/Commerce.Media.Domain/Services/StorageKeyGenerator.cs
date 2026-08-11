namespace Commerce.Media.Domain.Services;

public static class StorageKeyGenerator
{
    public static string Create(int storeId, string extension, DateTime utcNow)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        var normalizedExtension = NormalizeExtension(extension);
        var generatedId = Guid.NewGuid().ToString("N");
        return $"media/stores/{storeId}/{utcNow:yyyy}/{utcNow:MM}/{generatedId}.{normalizedExtension}";
    }

    public static string CreateThumbnailKey(string originalStorageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalStorageKey);

        if (originalStorageKey.Contains("..", StringComparison.Ordinal) ||
            originalStorageKey.IndexOf('\\') >= 0 ||
            originalStorageKey.StartsWith('/'))
        {
            throw new ArgumentException("Invalid storage key.", nameof(originalStorageKey));
        }

        var extension = GetExtension(originalStorageKey);
        var basePath = extension.Length > 0 ? originalStorageKey[..^extension.Length] : originalStorageKey;
        return $"{basePath}_thumb{extension}";
    }

    public static void ValidateStorageKey(string storageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        if (storageKey.Contains("..", StringComparison.Ordinal) ||
            storageKey.IndexOf('\\') >= 0 ||
            storageKey.StartsWith('/'))
        {
            throw new ArgumentException("Invalid storage key.");
        }
    }

    private static string GetExtension(string fileName)
    {
        var dot = fileName.LastIndexOf('.');
        return dot >= 0 ? fileName[dot..] : string.Empty;
    }

    private static string NormalizeExtension(string extension)
    {
        var normalized = extension.Trim().TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "bin";
        }

        if (normalized.Length > 10 || normalized.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new ArgumentException("Invalid file extension.");
        }

        return normalized;
    }
}
