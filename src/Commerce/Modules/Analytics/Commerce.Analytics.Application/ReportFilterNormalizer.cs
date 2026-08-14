using Commerce.Analytics.Application.Abstractions;
using Commerce.Analytics.Contracts;

namespace Commerce.Analytics.Application;

internal static class ReportFilterNormalizer
{
    private const int DefaultRangeDays = 30;
    private const int MaxTopProducts = 50;

    internal static AnalyticsFilterCriteria Normalize(ReportFilterQuery query)
    {
        var toUtc = query.ToUtc ?? DateTime.UtcNow;
        var fromUtc = query.FromUtc ?? toUtc.AddDays(-DefaultRangeDays);

        if (fromUtc > toUtc)
        {
            (fromUtc, toUtc) = (toUtc, fromUtc);
        }

        return new AnalyticsFilterCriteria(
            query.StoreId,
            fromUtc,
            toUtc,
            query.ProductId,
            query.CustomerId,
            query.Granularity,
            Math.Clamp(query.TopProductsLimit, 1, MaxTopProducts));
    }
}
