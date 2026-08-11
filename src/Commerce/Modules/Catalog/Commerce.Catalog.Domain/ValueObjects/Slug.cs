namespace Commerce.Catalog.Domain.ValueObjects;

public sealed record Slug
{
    public const int MaxLength = 200;

    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Slug is required.", nameof(value));
        }

        var normalized = Normalize(value);

        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException($"Slug cannot exceed {MaxLength} characters.", nameof(value));
        }

        return new Slug(normalized);
    }

    public static string Normalize(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        var chars = trimmed
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        var collapsed = new string(chars);
        while (collapsed.Contains("--", StringComparison.Ordinal))
        {
            collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
        }

        return collapsed.Trim('-');
    }

    public override string ToString() => Value;
}
