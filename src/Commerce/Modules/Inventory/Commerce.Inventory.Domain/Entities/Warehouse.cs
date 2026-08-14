using Commerce.Framework.Core.Entities;

namespace Commerce.Inventory.Domain.Entities;

public sealed class Warehouse : AggregateRoot
{
    public const int NameMaxLength = 128;
    public const int SystemNameMaxLength = 64;

    public int StoreId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string SystemName { get; private set; } = string.Empty;

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Warehouse Create(int storeId, string name, string systemName, bool isDefault, int displayOrder = 0)
    {
        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > NameMaxLength)
        {
            throw new ArgumentException("Warehouse name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(systemName) || systemName.Length > SystemNameMaxLength)
        {
            throw new ArgumentException("Warehouse system name is required.", nameof(systemName));
        }

        var utcNow = DateTime.UtcNow;
        return new Warehouse
        {
            StoreId = storeId,
            Name = name.Trim(),
            SystemName = systemName.Trim().ToLowerInvariant(),
            IsDefault = isDefault,
            IsActive = true,
            DisplayOrder = displayOrder,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(string name, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > NameMaxLength)
        {
            throw new ArgumentException("Warehouse name is required.", nameof(name));
        }

        Name = name.Trim();
        DisplayOrder = displayOrder;
        Touch();
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
