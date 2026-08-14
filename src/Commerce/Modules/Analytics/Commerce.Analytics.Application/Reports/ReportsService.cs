using System.Globalization;
using System.Text;
using Commerce.Analytics.Application.Abstractions;
using Commerce.Analytics.Contracts;
using Commerce.Framework.Core.Results;

namespace Commerce.Analytics.Application.Reports;

public sealed class ReportsService(IAnalyticsReadRepository repository) : IReportsService
{
    public async Task<Result<RevenueReportDto>> GetRevenueReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = ReportFilterNormalizer.Normalize(filter);
        var report = await repository.GetRevenueReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(report);
    }

    public async Task<Result<OrdersReportDto>> GetOrdersReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = ReportFilterNormalizer.Normalize(filter);
        var report = await repository.GetOrdersReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(report);
    }

    public async Task<Result<CustomersReportDto>> GetCustomersReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = ReportFilterNormalizer.Normalize(filter);
        var report = await repository.GetCustomersReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(report);
    }

    public async Task<Result<ProductsReportDto>> GetProductsReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = ReportFilterNormalizer.Normalize(filter);
        var report = await repository.GetProductsReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(report);
    }

    public async Task<Result<InventoryReportDto>> GetInventoryReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = ReportFilterNormalizer.Normalize(filter);
        var report = await repository.GetInventoryReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(report);
    }

    public async Task<Result<PaymentsReportDto>> GetPaymentsReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = ReportFilterNormalizer.Normalize(filter);
        var report = await repository.GetPaymentsReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(report);
    }

    public async Task<Result<RefundsReportDto>> GetRefundsReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = ReportFilterNormalizer.Normalize(filter);
        var report = await repository.GetRefundsReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(report);
    }

    public async Task<Result<DiscountsReportDto>> GetDiscountsReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = ReportFilterNormalizer.Normalize(filter);
        var report = await repository.GetDiscountsReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(report);
    }

    public async Task<Result<DownloadsReportDto>> GetDownloadsReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = ReportFilterNormalizer.Normalize(filter);
        var report = await repository.GetDownloadsReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(report);
    }

    public async Task<Result<ConversionReportDto>> GetConversionReportAsync(
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = ReportFilterNormalizer.Normalize(filter);
        var report = await repository.GetConversionReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        return Result.Success(report);
    }

    public async Task<Result<ReportExportDto>> ExportReportAsync(
        ReportType reportType,
        ReportFilterQuery filter,
        CancellationToken cancellationToken = default)
    {
        var criteria = ReportFilterNormalizer.Normalize(filter);
        var export = reportType switch
        {
            ReportType.Revenue => await ExportRevenueAsync(criteria, cancellationToken).ConfigureAwait(false),
            ReportType.Orders => await ExportOrdersAsync(criteria, cancellationToken).ConfigureAwait(false),
            ReportType.Customers => await ExportCustomersAsync(criteria, cancellationToken).ConfigureAwait(false),
            ReportType.Products => await ExportProductsAsync(criteria, cancellationToken).ConfigureAwait(false),
            ReportType.Inventory => await ExportInventoryAsync(criteria, cancellationToken).ConfigureAwait(false),
            ReportType.Payments => await ExportPaymentsAsync(criteria, cancellationToken).ConfigureAwait(false),
            ReportType.Refunds => await ExportRefundsAsync(criteria, cancellationToken).ConfigureAwait(false),
            ReportType.Discounts => await ExportDiscountsAsync(criteria, cancellationToken).ConfigureAwait(false),
            ReportType.Downloads => await ExportDownloadsAsync(criteria, cancellationToken).ConfigureAwait(false),
            ReportType.Conversion => await ExportConversionAsync(criteria, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(reportType), reportType, "Unsupported report type.")
        };

        return Result.Success(export);
    }

    private async Task<ReportExportDto> ExportRevenueAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var report = await repository.GetRevenueReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var builder = new StringBuilder();
        builder.AppendLine("metric,value");
        builder.AppendLine(CsvRow("gross_revenue", report.GrossRevenue));
        builder.AppendLine(CsvRow("discount_total", report.DiscountTotal));
        builder.AppendLine(CsvRow("tax_total", report.TaxTotal));
        builder.AppendLine(CsvRow("shipping_total", report.ShippingTotal));
        builder.AppendLine(CsvRow("net_revenue", report.NetRevenue));
        builder.AppendLine(CsvRow("paid_order_count", report.PaidOrderCount));
        builder.AppendLine();
        builder.AppendLine("period_start_utc,revenue,order_count");
        foreach (var point in report.TimeSeries)
        {
            builder.AppendLine($"{point.PeriodStartUtc:O},{FormatDecimal(point.Value)},{point.Count}");
        }

        return CreateExport(ReportType.Revenue, "revenue-report.csv", builder.ToString());
    }

    private async Task<ReportExportDto> ExportOrdersAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var report = await repository.GetOrdersReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var builder = new StringBuilder();
        builder.AppendLine("metric,value");
        builder.AppendLine(CsvRow("total_orders", report.TotalOrders));
        builder.AppendLine();
        builder.AppendLine("order_status,count");
        foreach (var row in report.ByOrderStatus)
        {
            builder.AppendLine($"{EscapeCsv(row.Status)},{row.Count}");
        }

        builder.AppendLine();
        builder.AppendLine("payment_status,count");
        foreach (var row in report.ByPaymentStatus)
        {
            builder.AppendLine($"{EscapeCsv(row.Status)},{row.Count}");
        }

        return CreateExport(ReportType.Orders, "orders-report.csv", builder.ToString());
    }

    private async Task<ReportExportDto> ExportCustomersAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var report = await repository.GetCustomersReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var builder = new StringBuilder();
        builder.AppendLine("metric,value");
        builder.AppendLine(CsvRow("new_customers", report.NewCustomers));
        builder.AppendLine(CsvRow("active_customers", report.ActiveCustomers));
        builder.AppendLine();
        builder.AppendLine("period_start_utc,new_customers");
        foreach (var point in report.RegistrationsTimeSeries)
        {
            builder.AppendLine($"{point.PeriodStartUtc:O},{point.Count}");
        }

        return CreateExport(ReportType.Customers, "customers-report.csv", builder.ToString());
    }

    private async Task<ReportExportDto> ExportProductsAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var report = await repository.GetProductsReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var builder = new StringBuilder();
        builder.AppendLine("metric,value");
        builder.AppendLine(CsvRow("published_products", report.PublishedProducts));
        builder.AppendLine(CsvRow("unpublished_products", report.UnpublishedProducts));
        builder.AppendLine();
        builder.AppendLine("product_id,product_name,quantity_sold,revenue,currency");
        foreach (var row in report.TopByRevenue)
        {
            builder.AppendLine($"{row.ProductId},{EscapeCsv(row.ProductName)},{row.QuantitySold},{FormatDecimal(row.Revenue)},{row.CurrencyCode}");
        }

        return CreateExport(ReportType.Products, "products-report.csv", builder.ToString());
    }

    private async Task<ReportExportDto> ExportInventoryAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var report = await repository.GetInventoryReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var builder = new StringBuilder();
        builder.AppendLine("metric,value");
        builder.AppendLine(CsvRow("tracked_items", report.TrackedItems));
        builder.AppendLine(CsvRow("low_stock_items", report.LowStockItems));
        builder.AppendLine(CsvRow("out_of_stock_items", report.OutOfStockItems));
        builder.AppendLine(CsvRow("total_on_hand", report.TotalOnHand));
        builder.AppendLine(CsvRow("total_reserved", report.TotalReserved));
        builder.AppendLine(CsvRow("total_available", report.TotalAvailable));
        return CreateExport(ReportType.Inventory, "inventory-report.csv", builder.ToString());
    }

    private async Task<ReportExportDto> ExportPaymentsAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var report = await repository.GetPaymentsReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var builder = new StringBuilder();
        builder.AppendLine("metric,value");
        builder.AppendLine(CsvRow("captured_amount", report.CapturedAmount));
        builder.AppendLine(CsvRow("authorized_amount", report.AuthorizedAmount));
        builder.AppendLine(CsvRow("failed_count", report.FailedCount));
        builder.AppendLine(CsvRow("captured_count", report.CapturedCount));
        builder.AppendLine();
        builder.AppendLine("provider,amount,count");
        foreach (var row in report.ByProvider)
        {
            builder.AppendLine($"{EscapeCsv(row.Status)},{FormatDecimal(row.Amount)},{row.Count}");
        }

        return CreateExport(ReportType.Payments, "payments-report.csv", builder.ToString());
    }

    private async Task<ReportExportDto> ExportRefundsAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var report = await repository.GetRefundsReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var builder = new StringBuilder();
        builder.AppendLine("metric,value");
        builder.AppendLine(CsvRow("refund_count", report.RefundCount));
        builder.AppendLine(CsvRow("refund_amount", report.RefundAmount));
        builder.AppendLine(CsvRow("succeeded_count", report.SucceededCount));
        builder.AppendLine(CsvRow("failed_count", report.FailedCount));
        return CreateExport(ReportType.Refunds, "refunds-report.csv", builder.ToString());
    }

    private async Task<ReportExportDto> ExportDiscountsAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var report = await repository.GetDiscountsReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var builder = new StringBuilder();
        builder.AppendLine("metric,value");
        builder.AppendLine(CsvRow("order_discount_total", report.OrderDiscountTotal));
        builder.AppendLine(CsvRow("coupon_usage_count", report.CouponUsageCount));
        builder.AppendLine(CsvRow("promotion_usage_count", report.PromotionUsageCount));
        return CreateExport(ReportType.Discounts, "discounts-report.csv", builder.ToString());
    }

    private async Task<ReportExportDto> ExportDownloadsAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var report = await repository.GetDownloadsReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var builder = new StringBuilder();
        builder.AppendLine("metric,value");
        builder.AppendLine(CsvRow("total_downloads", report.TotalDownloads));
        builder.AppendLine(CsvRow("successful_downloads", report.SuccessfulDownloads));
        builder.AppendLine(CsvRow("failed_downloads", report.FailedDownloads));
        return CreateExport(ReportType.Downloads, "downloads-report.csv", builder.ToString());
    }

    private async Task<ReportExportDto> ExportConversionAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var report = await repository.GetConversionReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var builder = new StringBuilder();
        builder.AppendLine("metric,value");
        builder.AppendLine(CsvRow("carts_created", report.CartsCreated));
        builder.AppendLine(CsvRow("checkouts_started", report.CheckoutsStarted));
        builder.AppendLine(CsvRow("orders_completed", report.OrdersCompleted));
        builder.AppendLine(CsvRow("cart_to_checkout_rate", report.CartToCheckoutRate));
        builder.AppendLine(CsvRow("checkout_to_order_rate", report.CheckoutToOrderRate));
        builder.AppendLine(CsvRow("cart_to_order_rate", report.CartToOrderRate));
        return CreateExport(ReportType.Conversion, "conversion-report.csv", builder.ToString());
    }

    private static ReportExportDto CreateExport(ReportType reportType, string fileName, string content) =>
        new(reportType, fileName, "text/csv", content);

    private static string CsvRow(string key, decimal value) =>
        $"{key},{FormatDecimal(value)}";

    private static string CsvRow(string key, int value) =>
        $"{key},{value.ToString(CultureInfo.InvariantCulture)}";

    private static string FormatDecimal(decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
