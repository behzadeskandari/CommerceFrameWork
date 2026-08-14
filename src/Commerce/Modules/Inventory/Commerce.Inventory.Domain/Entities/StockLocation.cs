using Commerce.Framework.Core.Entities;

namespace Commerce.Inventory.Domain.Entities;

public sealed class StockLocation : AggregateRoot
{
    public const int CodeMaxLength = 32;
    public const int NameMaxLength = 128;

    public int WarehouseId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static StockLocation Create(int warehouseId, string code, string name, bool isDefault)
    {
        if (warehouseId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(warehouseId));
        }

        if (string.IsNullOrWhiteSpace(code) || code.Length > CodeMaxLength)
        {
            throw new ArgumentException("Location code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > NameMaxLength)
        {
            throw new ArgumentException("Location name is required.", nameof(name));
        }

        var utcNow = DateTime.UtcNow;
        return new StockLocation
        {
            WarehouseId = warehouseId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            IsDefault = isDefault,
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > NameMaxLength)
        {
            throw new ArgumentException("Location name is required.", nameof(name));
        }

        Name = name.Trim();
        Touch();
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
