using Commerce.Framework.Core.Results;

namespace Commerce.Analytics.Contracts;

public enum ReportType
{
    Revenue = 0,
    Orders = 1,
    Customers = 2,
    Products = 3,
    Inventory = 4,
    Payments = 5,
    Refunds = 6,
    Discounts = 7,
    Downloads = 8,
    Conversion = 9
}

public enum ReportGranularity
{
    Day = 0,
    Week = 1,
    Month = 2
}

public sealed record ReportFilterQuery(
    int? StoreId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int? ProductId = null,
    int? CustomerId = null,
    ReportGranularity Granularity = ReportGranularity.Day,
    int TopProductsLimit = 10);

public sealed record MetricValueDto(string Key, decimal Value, string? Label = null);

public sealed record TimeSeriesPointDto(DateTime PeriodStartUtc, decimal Value, int Count = 0);

public sealed record StatusBreakdownDto(string Status, int Count, decimal Amount = 0m);

public sealed record TopProductRowDto(
    int ProductId,
    string ProductName,
    int QuantitySold,
    decimal Revenue,
    string CurrencyCode);

public sealed record DashboardSummaryDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int? StoreId,
    decimal TotalRevenue,
    int OrderCount,
    decimal AverageOrderValue,
    int NewCustomers,
    decimal TotalRefunded,
    int LowStockItems,
    int OutOfStockItems,
    decimal CartToOrderConversionRate,
    IReadOnlyList<TimeSeriesPointDto> RevenueTimeSeries,
    IReadOnlyList<StatusBreakdownDto> OrdersByStatus,
    IReadOnlyList<TopProductRowDto> TopProducts);

public sealed record RevenueReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    decimal GrossRevenue,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal ShippingTotal,
    decimal NetRevenue,
    int PaidOrderCount,
    IReadOnlyList<TimeSeriesPointDto> TimeSeries);

public sealed record OrdersReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int TotalOrders,
    IReadOnlyList<StatusBreakdownDto> ByOrderStatus,
    IReadOnlyList<StatusBreakdownDto> ByPaymentStatus,
    IReadOnlyList<StatusBreakdownDto> ByFulfillmentStatus,
    IReadOnlyList<TimeSeriesPointDto> TimeSeries);

public sealed record CustomersReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int NewCustomers,
    int ActiveCustomers,
    IReadOnlyList<TimeSeriesPointDto> RegistrationsTimeSeries);

public sealed record ProductsReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int PublishedProducts,
    int UnpublishedProducts,
    IReadOnlyList<TopProductRowDto> TopByRevenue,
    IReadOnlyList<TopProductRowDto> TopByQuantity);

public sealed record InventoryReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int TrackedItems,
    int LowStockItems,
    int OutOfStockItems,
    decimal TotalOnHand,
    decimal TotalReserved,
    decimal TotalAvailable);

public sealed record PaymentsReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    decimal CapturedAmount,
    decimal AuthorizedAmount,
    int FailedCount,
    int CapturedCount,
    IReadOnlyList<StatusBreakdownDto> ByProvider,
    IReadOnlyList<TimeSeriesPointDto> TimeSeries);

public sealed record RefundsReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int RefundCount,
    decimal RefundAmount,
    int SucceededCount,
    int FailedCount,
    IReadOnlyList<TimeSeriesPointDto> TimeSeries);

public sealed record DiscountsReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    decimal OrderDiscountTotal,
    int CouponUsageCount,
    int PromotionUsageCount,
    IReadOnlyList<TimeSeriesPointDto> TimeSeries);

public sealed record DownloadsReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int TotalDownloads,
    int SuccessfulDownloads,
    int FailedDownloads,
    IReadOnlyList<TimeSeriesPointDto> TimeSeries);

public sealed record ConversionReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int CartsCreated,
    int CheckoutsStarted,
    int OrdersCompleted,
    decimal CartToCheckoutRate,
    decimal CheckoutToOrderRate,
    decimal CartToOrderRate,
    IReadOnlyList<TimeSeriesPointDto> OrdersTimeSeries);

public sealed record ReportExportDto(
    ReportType ReportType,
    string FileName,
    string ContentType,
    string Content);

public interface IDashboardService
{
    Task<Result<DashboardSummaryDto>> GetSummaryAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);
}

public interface IReportsService
{
    Task<Result<RevenueReportDto>> GetRevenueReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);

    Task<Result<OrdersReportDto>> GetOrdersReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);

    Task<Result<CustomersReportDto>> GetCustomersReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);

    Task<Result<ProductsReportDto>> GetProductsReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);

    Task<Result<InventoryReportDto>> GetInventoryReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);

    Task<Result<PaymentsReportDto>> GetPaymentsReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);

    Task<Result<RefundsReportDto>> GetRefundsReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);

    Task<Result<DiscountsReportDto>> GetDiscountsReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);

    Task<Result<DownloadsReportDto>> GetDownloadsReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);

    Task<Result<ConversionReportDto>> GetConversionReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);

    Task<Result<ReportExportDto>> ExportReportAsync(
        ReportType reportType,
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default);
}
