using Commerce.Framework.Core.Entities;

namespace Commerce.Downloads.Domain.Entities;

public sealed class DownloadEntitlement : AggregateRoot
{
    public int OrderId { get; private set; }

    public int OrderItemId { get; private set; }

    public int ProductId { get; private set; }

    public int StoreId { get; private set; }

    public int? CustomerId { get; private set; }

    public string? GuestAccessToken { get; private set; }

    public DateTime GrantedAtUtc { get; private set; }

    public DateTime? ExpiresAtUtc { get; private set; }

    public int? MaxDownloadCount { get; private set; }

    public int DownloadCount { get; private set; }

    public bool IsRevoked { get; private set; }

    public static DownloadEntitlement Grant(
        int orderId,
        int orderItemId,
        int productId,
        int storeId,
        int? customerId,
        string? guestAccessToken,
        DateTime grantedAtUtc,
        DateTime? expiresAtUtc,
        int? maxDownloadCount)
    {
        if (orderId <= 0 || orderItemId <= 0 || productId <= 0 || storeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderId));
        }

        return new DownloadEntitlement
        {
            OrderId = orderId,
            OrderItemId = orderItemId,
            ProductId = productId,
            StoreId = storeId,
            CustomerId = customerId,
            GuestAccessToken = guestAccessToken,
            GrantedAtUtc = grantedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            MaxDownloadCount = maxDownloadCount,
            DownloadCount = 0,
            IsRevoked = false
        };
    }

    public bool IsOwnedByCustomer(int customerId) =>
        CustomerId.HasValue && CustomerId.Value == customerId;

    public bool IsAccessibleByGuest(string accessToken) =>
        !CustomerId.HasValue &&
        !string.IsNullOrWhiteSpace(GuestAccessToken) &&
        !string.IsNullOrWhiteSpace(accessToken) &&
        string.Equals(GuestAccessToken, accessToken, StringComparison.Ordinal);

    public bool IsExpired(DateTime utcNow) =>
        ExpiresAtUtc.HasValue && utcNow > ExpiresAtUtc.Value;

    public bool HasRemainingDownloads() =>
        !MaxDownloadCount.HasValue || DownloadCount < MaxDownloadCount.Value;

    public void RecordSuccessfulDownload(DateTime utcNow)
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException("Download entitlement is revoked.");
        }

        if (IsExpired(utcNow))
        {
            throw new InvalidOperationException("Download entitlement has expired.");
        }

        if (!HasRemainingDownloads())
        {
            throw new InvalidOperationException("Download limit exceeded.");
        }

        DownloadCount++;
    }

    public void Revoke() => IsRevoked = true;
}
