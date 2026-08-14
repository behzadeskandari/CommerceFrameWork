using Commerce.Analytics.Application.Abstractions;
using Commerce.Analytics.Contracts;
using Commerce.Framework.Core.Results;

namespace Commerce.Analytics.Application.Dashboard;

public sealed class DashboardService(IAnalyticsReadRepository repository) : IDashboardService
{
    public async Task<Result<DashboardSummaryDto>> GetSummaryAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = ReportFilterNormalizer.Normalize(filter);
        var summary = await repository.GetDashboardSummaryAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(summary);
    }
}
