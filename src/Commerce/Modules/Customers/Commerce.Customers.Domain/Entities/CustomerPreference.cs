using Commerce.Framework.Core.Entities;

namespace Commerce.Customers.Domain.Entities;

public sealed class CustomerPreference : Entity
{
    public const int KeyMaxLength = 128;
    public const int ValueMaxLength = 2000;

    private CustomerPreference()
    {
    }

    public int CustomerId { get; private set; }

    public int? StoreId { get; private set; }

    public string PreferenceKey { get; private set; } = string.Empty;

    public string PreferenceValue { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static CustomerPreference Create(
        int customerId,
        int? storeId,
        string preferenceKey,
        string preferenceValue)
    {
        if (customerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(customerId));
        }

        if (string.IsNullOrWhiteSpace(preferenceKey))
        {
            throw new ArgumentException("Preference key is required.", nameof(preferenceKey));
        }

        var utcNow = DateTime.UtcNow;
        return new CustomerPreference
        {
            CustomerId = customerId,
            StoreId = storeId is > 0 ? storeId : null,
            PreferenceKey = preferenceKey.Trim(),
            PreferenceValue = preferenceValue.Trim(),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void UpdateValue(string preferenceValue)
    {
        PreferenceValue = preferenceValue.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
