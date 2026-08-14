namespace Commerce.Framework.Seo;

public static class SlugNormalizer
{
    public static string Normalize(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Slug is required.", nameof(slug));
        }

        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Contains("../", StringComparison.Ordinal) ||
            normalized.Contains('\\') ||
            normalized.Split('/').Any(x => x is "." or ".."))
        {
            throw new ArgumentException("Slug contains invalid path segments.", nameof(slug));
        }

        return normalized;
    }

    public static string FromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        var slug = new string(title
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
