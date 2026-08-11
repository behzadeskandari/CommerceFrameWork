using Commerce.Framework.Core.Entities;
using Commerce.Framework.Domain.ValueObjects;

namespace Commerce.Store.Domain.Entities;

public sealed class StoreCurrency : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 5;
    public const int SymbolMaxLength = 10;

    private StoreCurrency()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Symbol { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public int DecimalPlaces { get; private set; }

    public decimal Rate { get; private set; }

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static StoreCurrency Create(
        string code,
        string name,
        string symbol,
        string displayName,
        decimal rate,
        int decimalPlaces = 2,
        int displayOrder = 0,
        bool isActive = true)
    {
        _ = Currency.FromCode(code);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (rate < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rate));
        }

        var now = DateTime.UtcNow;
        var normalizedCode = code.Trim().ToUpperInvariant();
        return new StoreCurrency
        {
            Code = normalizedCode,
            Name = name.Trim(),
            Symbol = string.IsNullOrWhiteSpace(symbol) ? normalizedCode : symbol.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? name.Trim() : displayName.Trim(),
            Rate = rate,
            DecimalPlaces = decimalPlaces,
            DisplayOrder = displayOrder,
            IsActive = isActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(
        string name,
        string symbol,
        string displayName,
        decimal rate,
        int decimalPlaces,
        int displayOrder,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (rate < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rate));
        }

        Name = name.Trim();
        Symbol = string.IsNullOrWhiteSpace(symbol) ? Code : symbol.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? name.Trim() : displayName.Trim();
        Rate = rate;
        DecimalPlaces = decimalPlaces;
        DisplayOrder = displayOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
