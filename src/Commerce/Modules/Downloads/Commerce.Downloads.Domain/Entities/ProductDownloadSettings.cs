using Commerce.Framework.Core.Entities;

namespace Commerce.Downloads.Domain.Entities;

public sealed class ProductDownloadSettings : AggregateRoot
{
    public int ProductId { get; private set; }

    public int StoreId { get; private set; }

    public bool IsEnabled { get; private set; }

    public int? MaxDownloadCount { get; private set; }

    public int? ExpirationDays { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static ProductDownloadSettings Create(
        int productId,
        int storeId,
        bool isEnabled,
        int? maxDownloadCount,
        int? expirationDays)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        if (storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(storeId));
        }

        ValidateLimits(maxDownloadCount, expirationDays);

        var now = DateTime.UtcNow;
        return new ProductDownloadSettings
        {
            ProductId = productId,
            StoreId = storeId,
            IsEnabled = isEnabled,
            MaxDownloadCount = maxDownloadCount,
            ExpirationDays = expirationDays,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(bool isEnabled, int? maxDownloadCount, int? expirationDays)
    {
        ValidateLimits(maxDownloadCount, expirationDays);
        IsEnabled = isEnabled;
        MaxDownloadCount = maxDownloadCount;
        ExpirationDays = expirationDays;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public DateTime? CalculateExpirationUtc(DateTime grantedAtUtc) =>
        ExpirationDays.HasValue ? grantedAtUtc.AddDays(ExpirationDays.Value) : null;

    private static void ValidateLimits(int? maxDownloadCount, int? expirationDays)
    {
        if (maxDownloadCount is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDownloadCount), "Max download count must be positive or null.");
        }

        if (expirationDays is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expirationDays), "Expiration days must be positive or null.");
        }
    }
}
