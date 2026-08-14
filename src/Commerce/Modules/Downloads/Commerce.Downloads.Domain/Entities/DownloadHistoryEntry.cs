using Commerce.Framework.Core.Entities;

namespace Commerce.Downloads.Domain.Entities;

public sealed class DownloadHistoryEntry : Entity
{
    public const int IpAddressMaxLength = 45;
    public const int UserAgentMaxLength = 512;
    public const int FailureReasonMaxLength = 500;

    public int EntitlementId { get; private set; }

    public int ProductDownloadFileId { get; private set; }

    public int? CustomerId { get; private set; }

    public DateTime DownloadedAtUtc { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public bool WasSuccessful { get; private set; }

    public string? FailureReason { get; private set; }

    public static DownloadHistoryEntry Record(
        int entitlementId,
        int productDownloadFileId,
        int? customerId,
        DateTime downloadedAtUtc,
        bool wasSuccessful,
        string? ipAddress = null,
        string? userAgent = null,
        string? failureReason = null) =>
        new()
        {
            EntitlementId = entitlementId,
            ProductDownloadFileId = productDownloadFileId,
            CustomerId = customerId,
            DownloadedAtUtc = downloadedAtUtc,
            WasSuccessful = wasSuccessful,
            IpAddress = Normalize(ipAddress, IpAddressMaxLength),
            UserAgent = Normalize(userAgent, UserAgentMaxLength),
            FailureReason = wasSuccessful ? null : Normalize(failureReason, FailureReasonMaxLength)
        };

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
