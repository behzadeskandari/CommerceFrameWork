using Commerce.Framework.Core.Entities;

namespace Commerce.Pricing.Domain.Entities;

public sealed class CustomerGroup : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 50;

    public int StoreId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static CustomerGroup Create(
        int storeId,
        string name,
        string code,
        bool isActive = true,
        int displayOrder = 0)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        return new CustomerGroup
        {
            StoreId = storeId,
            Name = NormalizeRequired(name, NameMaxLength, nameof(name)),
            Code = NormalizeCode(code),
            IsActive = isActive,
            DisplayOrder = displayOrder,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(string name, string code, bool isActive, int displayOrder)
    {
        Name = NormalizeRequired(name, NameMaxLength, nameof(name));
        Code = NormalizeCode(code);
        IsActive = isActive;
        DisplayOrder = displayOrder;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeRequired(string value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        var trimmed = code.Trim().ToUpperInvariant();
        return trimmed.Length > CodeMaxLength ? trimmed[..CodeMaxLength] : trimmed;
    }
}
