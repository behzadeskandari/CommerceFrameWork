using Commerce.Customers.Domain.Enums;
using Commerce.Framework.Core.Entities;

namespace Commerce.Customers.Domain.Entities;

public sealed class CustomerActivityLog : Entity
{
    public const int SummaryMaxLength = 500;
    public const int DetailsMaxLength = 4000;

    private CustomerActivityLog()
    {
    }

    public int CustomerId { get; private set; }

    public int? StoreId { get; private set; }

    public CustomerActivityType ActivityType { get; private set; }

    public string Summary { get; private set; } = string.Empty;

    public string? DetailsJson { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static CustomerActivityLog Create(
        int customerId,
        int? storeId,
        CustomerActivityType activityType,
        string summary,
        string? detailsJson = null)
    {
        if (customerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(customerId));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Activity summary is required.", nameof(summary));
        }

        return new CustomerActivityLog
        {
            CustomerId = customerId,
            StoreId = storeId is > 0 ? storeId : null,
            ActivityType = activityType,
            Summary = summary.Trim(),
            DetailsJson = string.IsNullOrWhiteSpace(detailsJson) ? null : detailsJson.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
