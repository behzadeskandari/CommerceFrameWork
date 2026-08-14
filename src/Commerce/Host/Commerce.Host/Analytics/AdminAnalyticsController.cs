using Commerce.Analytics.Contracts;
using Commerce.Analytics.Infrastructure.Security;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.Host.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Host.Analytics;

[ApiController]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(AnalyticsPermissions.View)]
    public async Task<IActionResult> GetSummary([FromQuery] ReportFilterQuery query, CancellationToken cancellationToken)
    {
        var result = await dashboardService.GetSummaryAsync(query, cancellationToken).ConfigureAwait(false);
        return AnalyticsActionResults.ToActionResult(this, result, value => value);
    }
}

[ApiController]
[Route("api/admin/reports")]
public sealed class AdminReportsController(IReportsService reportsService) : ControllerBase
{
    [HttpGet("revenue")]
    [RequirePermission(AnalyticsPermissions.ReportsView)]
    public Task<IActionResult> GetRevenue([FromQuery] ReportFilterQuery query, CancellationToken cancellationToken) =>
        GetReportAsync(reportsService.GetRevenueReportAsync(query, cancellationToken));

    [HttpGet("orders")]
    [RequirePermission(AnalyticsPermissions.ReportsView)]
    public Task<IActionResult> GetOrders([FromQuery] ReportFilterQuery query, CancellationToken cancellationToken) =>
        GetReportAsync(reportsService.GetOrdersReportAsync(query, cancellationToken));

    [HttpGet("customers")]
    [RequirePermission(AnalyticsPermissions.ReportsView)]
    public Task<IActionResult> GetCustomers([FromQuery] ReportFilterQuery query, CancellationToken cancellationToken) =>
        GetReportAsync(reportsService.GetCustomersReportAsync(query, cancellationToken));

    [HttpGet("products")]
    [RequirePermission(AnalyticsPermissions.ReportsView)]
    public Task<IActionResult> GetProducts([FromQuery] ReportFilterQuery query, CancellationToken cancellationToken) =>
        GetReportAsync(reportsService.GetProductsReportAsync(query, cancellationToken));

    [HttpGet("inventory")]
    [RequirePermission(AnalyticsPermissions.ReportsView)]
    public Task<IActionResult> GetInventory([FromQuery] ReportFilterQuery query, CancellationToken cancellationToken) =>
        GetReportAsync(reportsService.GetInventoryReportAsync(query, cancellationToken));

    [HttpGet("payments")]
    [RequirePermission(AnalyticsPermissions.ReportsView)]
    public Task<IActionResult> GetPayments([FromQuery] ReportFilterQuery query, CancellationToken cancellationToken) =>
        GetReportAsync(reportsService.GetPaymentsReportAsync(query, cancellationToken));

    [HttpGet("refunds")]
    [RequirePermission(AnalyticsPermissions.ReportsView)]
    public Task<IActionResult> GetRefunds([FromQuery] ReportFilterQuery query, CancellationToken cancellationToken) =>
        GetReportAsync(reportsService.GetRefundsReportAsync(query, cancellationToken));

    [HttpGet("discounts")]
    [RequirePermission(AnalyticsPermissions.ReportsView)]
    public Task<IActionResult> GetDiscounts([FromQuery] ReportFilterQuery query, CancellationToken cancellationToken) =>
        GetReportAsync(reportsService.GetDiscountsReportAsync(query, cancellationToken));

    [HttpGet("downloads")]
    [RequirePermission(AnalyticsPermissions.ReportsView)]
    public Task<IActionResult> GetDownloads([FromQuery] ReportFilterQuery query, CancellationToken cancellationToken) =>
        GetReportAsync(reportsService.GetDownloadsReportAsync(query, cancellationToken));

    [HttpGet("conversion")]
    [RequirePermission(AnalyticsPermissions.ReportsView)]
    public Task<IActionResult> GetConversion([FromQuery] ReportFilterQuery query, CancellationToken cancellationToken) =>
        GetReportAsync(reportsService.GetConversionReportAsync(query, cancellationToken));

    [HttpGet("{reportType}/export")]
    [RequirePermission(AnalyticsPermissions.ReportsExport)]
    public async Task<IActionResult> Export(
        ReportType reportType,
        [FromQuery] ReportFilterQuery query,
        CancellationToken cancellationToken)
    {
        var result = await reportsService.ExportReportAsync(reportType, query, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return AnalyticsActionResults.ToActionResult(this, result, value => value);
        }

        var export = result.Value!;
        return File(
            System.Text.Encoding.UTF8.GetBytes(export.Content),
            export.ContentType,
            export.FileName);
    }

    private async Task<IActionResult> GetReportAsync<T>(Task<Result<T>> task)
    {
        var result = await task.ConfigureAwait(false);
        return AnalyticsActionResults.ToActionResult(this, result, value => value);
    }
}

internal static class AnalyticsActionResults
{
    internal static IActionResult ToActionResult<T>(
        ControllerBase controller,
        Result<T> result,
        Func<T, object?> dataSelector,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return controller.StatusCode(successStatusCode, new { success = true, data = dataSelector(result.Value!) });
        }

        return MapFailure(controller, result.Error!);
    }

    private static IActionResult MapFailure(ControllerBase controller, Error error) =>
        error.Type switch
        {
            ErrorType.NotFound => controller.NotFound(new { success = false, error = error.Message }),
            ErrorType.Conflict => controller.Conflict(new { success = false, error = error.Message }),
            _ => controller.BadRequest(new { success = false, error = error.Message })
        };
}
