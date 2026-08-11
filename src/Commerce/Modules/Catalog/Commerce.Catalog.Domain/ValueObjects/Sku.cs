namespace Commerce.Catalog.Domain.ValueObjects;

public sealed record Sku
{
    public const int MaxLength = 64;

    public string Value { get; }

    private Sku(string value) => Value = value;

    public static Sku Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SKU is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException($"SKU cannot exceed {MaxLength} characters.", nameof(value));
        }

        return new Sku(normalized);
    }

    public override string ToString() => Value;
}
