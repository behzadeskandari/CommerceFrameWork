using Commerce.Analytics.Contracts;

namespace Commerce.Analytics.Application.Abstractions;

public sealed record AnalyticsFilterCriteria(
    int? StoreId,
    DateTime FromUtc,
    DateTime ToUtc,
    int? ProductId,
    int? CustomerId,
    ReportGranularity Granularity,
    int TopProductsLimit);

public interface IAnalyticsReadRepository
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<RevenueReportDto> GetRevenueReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<OrdersReportDto> GetOrdersReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<CustomersReportDto> GetCustomersReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<ProductsReportDto> GetProductsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<InventoryReportDto> GetInventoryReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<PaymentsReportDto> GetPaymentsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<RefundsReportDto> GetRefundsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<DiscountsReportDto> GetDiscountsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<DownloadsReportDto> GetDownloadsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<ConversionReportDto> GetConversionReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default);
}
