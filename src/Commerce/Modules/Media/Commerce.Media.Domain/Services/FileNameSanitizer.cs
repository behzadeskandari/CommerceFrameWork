namespace Commerce.Media.Domain.Services;

public static class FileNameSanitizer
{
    public const int MaxFileNameLength = 255;

    public static string SanitizeOriginalFileName(string? originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return "upload";
        }

        var name = originalFileName.Trim();
        name = name.Replace('\\', '/');
        var lastSlash = name.LastIndexOf('/');
        if (lastSlash >= 0)
        {
            name = name[(lastSlash + 1)..];
        }

        if (name.Contains('\0', StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid file name.");
        }

        if (name.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid file name.");
        }

        var invalidChars = new[] { '<', '>', ':', '"', '|', '?', '*', '\0' };
        name = new string(name.Where(ch => !invalidChars.Contains(ch)).ToArray());

        if (string.IsNullOrWhiteSpace(name))
        {
            return "upload";
        }

        return name.Length > MaxFileNameLength ? name[..MaxFileNameLength] : name;
    }

    public static string GetSafeExtension(string? originalFileName, string? contentType)
    {
        var extension = GetExtension(originalFileName ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(extension) && IsAllowedExtension(extension))
        {
            return extension;
        }

        return contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/gif" => "gif",
            "image/webp" => "webp",
            "application/pdf" => "pdf",
            _ => "bin"
        };
    }

    private static bool IsAllowedExtension(string extension) =>
        extension is "jpg" or "jpeg" or "png" or "gif" or "webp" or "pdf";

    private static string GetExtension(string fileName)
    {
        var dot = fileName.LastIndexOf('.');
        return dot >= 0 ? fileName[dot..] : string.Empty;
    }
}
