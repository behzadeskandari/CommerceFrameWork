using Commerce.Analytics.Application.Abstractions;
using Commerce.Analytics.Contracts;
using Commerce.Cart.Domain.Entities;
using Commerce.Catalog.Domain.Entities;
using Commerce.Checkout.Domain.Entities;
using Commerce.Customers.Domain.Entities;
using Commerce.Downloads.Domain.Entities;
using Commerce.Framework.Data.Db;
using Commerce.Inventory.Domain.Entities;
using Commerce.Orders.Domain.Entities;
using Commerce.Orders.Domain.Enums;
using Commerce.Payments.Domain.Entities;
using Commerce.Payments.Domain.Enums;
using Commerce.Pricing.Domain.Entities;
using Commerce.Promotions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using OrderPaymentStatus = Commerce.Orders.Domain.Enums.PaymentStatus;
using PaymentRecordStatus = Commerce.Payments.Domain.Enums.PaymentStatus;

namespace Commerce.Analytics.Infrastructure.Persistence.Repositories;

public sealed class EfAnalyticsReadRepository(CommerceDbContext dbContext) : IAnalyticsReadRepository
{
    public Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default) =>
        BuildDashboardSummaryAsync(criteria, cancellationToken);

    public Task<RevenueReportDto> GetRevenueReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default) =>
        BuildRevenueReportAsync(criteria, cancellationToken);

    public Task<OrdersReportDto> GetOrdersReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default) =>
        BuildOrdersReportAsync(criteria, cancellationToken);

    public Task<CustomersReportDto> GetCustomersReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default) =>
        BuildCustomersReportAsync(criteria, cancellationToken);

    public Task<ProductsReportDto> GetProductsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default) =>
        BuildProductsReportAsync(criteria, cancellationToken);

    public Task<InventoryReportDto> GetInventoryReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default) =>
        BuildInventoryReportAsync(criteria, cancellationToken);

    public Task<PaymentsReportDto> GetPaymentsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default) =>
        BuildPaymentsReportAsync(criteria, cancellationToken);

    public Task<RefundsReportDto> GetRefundsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default) =>
        BuildRefundsReportAsync(criteria, cancellationToken);

    public Task<DiscountsReportDto> GetDiscountsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default) =>
        BuildDiscountsReportAsync(criteria, cancellationToken);

    public Task<DownloadsReportDto> GetDownloadsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default) =>
        BuildDownloadsReportAsync(criteria, cancellationToken);

    public Task<ConversionReportDto> GetConversionReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken = default) =>
        BuildConversionReportAsync(criteria, cancellationToken);

    private async Task<DashboardSummaryDto> BuildDashboardSummaryAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var revenue = await BuildRevenueReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var orders = await BuildOrdersReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var customers = await BuildCustomersReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var products = await BuildProductsReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var inventory = await BuildInventoryReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var refunds = await BuildRefundsReportAsync(criteria, cancellationToken).ConfigureAwait(false);
        var conversion = await BuildConversionReportAsync(criteria, cancellationToken).ConfigureAwait(false);

        var averageOrderValue = revenue.PaidOrderCount > 0
            ? revenue.NetRevenue / revenue.PaidOrderCount
            : 0m;

        return new DashboardSummaryDto(
            criteria.FromUtc,
            criteria.ToUtc,
            criteria.StoreId,
            revenue.NetRevenue,
            orders.TotalOrders,
            averageOrderValue,
            customers.NewCustomers,
            refunds.RefundAmount,
            inventory.LowStockItems,
            inventory.OutOfStockItems,
            conversion.CartToOrderRate,
            revenue.TimeSeries,
            orders.ByOrderStatus,
            products.TopByRevenue);
    }

    private async Task<RevenueReportDto> BuildRevenueReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var orders = FilterOrders(criteria)
            .Where(x =>
                x.PaymentStatus == OrderPaymentStatus.Paid &&
                x.Status != OrderStatus.Cancelled);

        var aggregates = await orders
            .GroupBy(_ => 1)
            .Select(g => new
            {
                GrossRevenue = g.Sum(x => x.GrandTotal),
                DiscountTotal = g.Sum(x => x.DiscountTotal),
                TaxTotal = g.Sum(x => x.TaxTotal),
                ShippingTotal = g.Sum(x => x.ShippingTotal),
                PaidOrderCount = g.Count()
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var gross = aggregates?.GrossRevenue ?? 0m;
        var discount = aggregates?.DiscountTotal ?? 0m;
        var tax = aggregates?.TaxTotal ?? 0m;
        var shipping = aggregates?.ShippingTotal ?? 0m;
        var paidCount = aggregates?.PaidOrderCount ?? 0;
        var timeSeries = await BuildOrderRevenueTimeSeriesAsync(orders, criteria, cancellationToken).ConfigureAwait(false);

        return new RevenueReportDto(
            criteria.FromUtc,
            criteria.ToUtc,
            gross,
            discount,
            tax,
            shipping,
            gross,
            paidCount,
            timeSeries);
    }

    private async Task<OrdersReportDto> BuildOrdersReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var orders = FilterOrders(criteria);

        var totalOrders = await orders.CountAsync(cancellationToken).ConfigureAwait(false);

        var byOrderStatus = await orders
            .GroupBy(x => x.Status)
            .Select(g => new StatusBreakdownDto(g.Key.ToString(), g.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byPaymentStatus = await orders
            .GroupBy(x => x.PaymentStatus)
            .Select(g => new StatusBreakdownDto(g.Key.ToString(), g.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byFulfillmentStatus = await orders
            .GroupBy(x => x.FulfillmentStatus)
            .Select(g => new StatusBreakdownDto(g.Key.ToString(), g.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var timeSeries = await BuildOrderCountTimeSeriesAsync(orders, criteria, cancellationToken).ConfigureAwait(false);

        return new OrdersReportDto(
            criteria.FromUtc,
            criteria.ToUtc,
            totalOrders,
            byOrderStatus,
            byPaymentStatus,
            byFulfillmentStatus,
            timeSeries);
    }

    private async Task<CustomersReportDto> BuildCustomersReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var customers = dbContext.Set<Customer>().AsNoTracking().Where(x => !x.Deleted);

        if (criteria.CustomerId.HasValue)
        {
            customers = customers.Where(x => x.Id == criteria.CustomerId.Value);
        }

        var newCustomers = await customers
            .CountAsync(x => x.CreatedAtUtc >= criteria.FromUtc && x.CreatedAtUtc <= criteria.ToUtc, cancellationToken)
            .ConfigureAwait(false);

        var activeCustomers = await customers
            .CountAsync(x => x.Active, cancellationToken)
            .ConfigureAwait(false);

        var registrations = await customers
            .Where(x => x.CreatedAtUtc >= criteria.FromUtc && x.CreatedAtUtc <= criteria.ToUtc)
            .Select(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var timeSeries = BuildTimeSeriesFromDates(registrations, criteria, countOnly: true);

        return new CustomersReportDto(
            criteria.FromUtc,
            criteria.ToUtc,
            newCustomers,
            activeCustomers,
            timeSeries);
    }

    private async Task<ProductsReportDto> BuildProductsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var products = dbContext.Set<Product>().AsNoTracking().Where(x => !x.Deleted);

        var publishedProducts = await products.CountAsync(x => x.Published, cancellationToken).ConfigureAwait(false);
        var unpublishedProducts = await products.CountAsync(x => !x.Published, cancellationToken).ConfigureAwait(false);

        var topByRevenue = await BuildTopProductsAsync(criteria, sortByQuantity: false, cancellationToken).ConfigureAwait(false);
        var topByQuantity = await BuildTopProductsAsync(criteria, sortByQuantity: true, cancellationToken).ConfigureAwait(false);

        return new ProductsReportDto(
            criteria.FromUtc,
            criteria.ToUtc,
            publishedProducts,
            unpublishedProducts,
            topByRevenue,
            topByQuantity);
    }

    private async Task<InventoryReportDto> BuildInventoryReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var inventory = dbContext.Set<InventoryItem>().AsNoTracking().Where(x => x.TrackInventory);

        if (criteria.StoreId.HasValue)
        {
            inventory = inventory.Where(x => x.StoreId == criteria.StoreId.Value);
        }

        if (criteria.ProductId.HasValue)
        {
            inventory = inventory.Where(x => x.ProductId == criteria.ProductId.Value);
        }

        var rows = await inventory
            .Select(x => new
            {
                x.OnHand,
                x.Reserved,
                x.Available,
                x.LowStockThreshold,
                IsLowStock = x.Available > 0 && x.Available <= x.LowStockThreshold,
                IsOutOfStock = x.Available <= 0
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new InventoryReportDto(
            criteria.FromUtc,
            criteria.ToUtc,
            rows.Count,
            rows.Count(x => x.IsLowStock),
            rows.Count(x => x.IsOutOfStock),
            rows.Sum(x => x.OnHand),
            rows.Sum(x => x.Reserved),
            rows.Sum(x => x.Available));
    }

    private async Task<PaymentsReportDto> BuildPaymentsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var payments = FilterPayments(criteria);

        var capturedAmount = await payments
            .Where(x => x.Status == PaymentRecordStatus.Captured || x.Status == PaymentRecordStatus.PartiallyRefunded || x.Status == PaymentRecordStatus.Refunded)
            .SumAsync(x => x.Amount, cancellationToken)
            .ConfigureAwait(false);

        var authorizedAmount = await payments
            .Where(x => x.Status == PaymentRecordStatus.Authorized)
            .SumAsync(x => x.Amount, cancellationToken)
            .ConfigureAwait(false);

        var failedCount = await payments
            .CountAsync(x => x.Status == PaymentRecordStatus.Failed, cancellationToken)
            .ConfigureAwait(false);

        var capturedCount = await payments
            .CountAsync(x => x.Status == PaymentRecordStatus.Captured || x.Status == PaymentRecordStatus.PartiallyRefunded || x.Status == PaymentRecordStatus.Refunded, cancellationToken)
            .ConfigureAwait(false);

        var byProvider = await payments
            .Where(x => x.Status == PaymentRecordStatus.Captured || x.Status == PaymentRecordStatus.PartiallyRefunded || x.Status == PaymentRecordStatus.Refunded)
            .GroupBy(x => x.ProviderSystemName)
            .Select(g => new StatusBreakdownDto(g.Key, g.Count(), g.Sum(x => x.Amount)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var paymentDates = await payments
            .Where(x => x.Status == PaymentRecordStatus.Captured || x.Status == PaymentRecordStatus.PartiallyRefunded || x.Status == PaymentRecordStatus.Refunded)
            .Select(x => new { x.CreatedAtUtc, x.Amount })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var timeSeries = BuildTimeSeriesFromAmounts(paymentDates.Select(x => (x.CreatedAtUtc, x.Amount)).ToList(), criteria);

        return new PaymentsReportDto(
            criteria.FromUtc,
            criteria.ToUtc,
            capturedAmount,
            authorizedAmount,
            failedCount,
            capturedCount,
            byProvider,
            timeSeries);
    }

    private async Task<RefundsReportDto> BuildRefundsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var refunds = dbContext.Set<Refund>().AsNoTracking()
            .Where(x => x.CreatedAtUtc >= criteria.FromUtc && x.CreatedAtUtc <= criteria.ToUtc);

        if (criteria.StoreId.HasValue || criteria.CustomerId.HasValue)
        {
            var payments = FilterPayments(criteria).Select(x => x.Id);
            refunds = refunds.Where(x => payments.Contains(x.PaymentId));
        }

        var refundCount = await refunds.CountAsync(cancellationToken).ConfigureAwait(false);
        var refundAmount = await refunds
            .Where(x => x.Status == RefundStatus.Succeeded)
            .SumAsync(x => x.Amount, cancellationToken)
            .ConfigureAwait(false);
        var succeededCount = await refunds.CountAsync(x => x.Status == RefundStatus.Succeeded, cancellationToken).ConfigureAwait(false);
        var failedCount = await refunds.CountAsync(x => x.Status == RefundStatus.Failed, cancellationToken).ConfigureAwait(false);

        var refundDates = await refunds
            .Where(x => x.Status == RefundStatus.Succeeded)
            .Select(x => new { x.CreatedAtUtc, x.Amount })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var timeSeries = BuildTimeSeriesFromAmounts(refundDates.Select(x => (x.CreatedAtUtc, x.Amount)).ToList(), criteria);

        return new RefundsReportDto(
            criteria.FromUtc,
            criteria.ToUtc,
            refundCount,
            refundAmount,
            succeededCount,
            failedCount,
            timeSeries);
    }

    private async Task<DiscountsReportDto> BuildDiscountsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var orders = FilterOrders(criteria)
            .Where(x => x.Status != OrderStatus.Cancelled);

        var orderDiscountTotal = await orders.SumAsync(x => x.DiscountTotal, cancellationToken).ConfigureAwait(false);

        var couponUsage = dbContext.Set<CouponUsage>().AsNoTracking()
            .Where(x => x.UsedAtUtc >= criteria.FromUtc && x.UsedAtUtc <= criteria.ToUtc);

        if (criteria.CustomerId.HasValue)
        {
            couponUsage = couponUsage.Where(x => x.CustomerId == criteria.CustomerId.Value);
        }

        if (criteria.StoreId.HasValue)
        {
            var orderIds = FilterOrders(criteria).Select(x => x.Id);
            couponUsage = couponUsage.Where(x => orderIds.Contains(x.OrderId));
        }

        var couponUsageCount = await couponUsage.CountAsync(cancellationToken).ConfigureAwait(false);

        var promotionUsage = dbContext.Set<PromotionUsage>().AsNoTracking()
            .Where(x => x.UsedAtUtc >= criteria.FromUtc && x.UsedAtUtc <= criteria.ToUtc);

        if (criteria.CustomerId.HasValue)
        {
            promotionUsage = promotionUsage.Where(x => x.CustomerId == criteria.CustomerId.Value);
        }

        var promotionUsageCount = await promotionUsage.CountAsync(cancellationToken).ConfigureAwait(false);

        var discountDates = await orders
            .Where(x => x.DiscountTotal > 0)
            .Select(x => new { x.CreatedAtUtc, x.DiscountTotal })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var timeSeries = BuildTimeSeriesFromAmounts(
            discountDates.Select(x => (x.CreatedAtUtc, x.DiscountTotal)).ToList(),
            criteria);

        return new DiscountsReportDto(
            criteria.FromUtc,
            criteria.ToUtc,
            orderDiscountTotal,
            couponUsageCount,
            promotionUsageCount,
            timeSeries);
    }

    private async Task<DownloadsReportDto> BuildDownloadsReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var downloads = dbContext.Set<DownloadHistoryEntry>().AsNoTracking()
            .Where(x => x.DownloadedAtUtc >= criteria.FromUtc && x.DownloadedAtUtc <= criteria.ToUtc);

        if (criteria.CustomerId.HasValue)
        {
            downloads = downloads.Where(x => x.CustomerId == criteria.CustomerId.Value);
        }

        if (criteria.ProductId.HasValue)
        {
            var fileIds = dbContext.Set<ProductDownloadFile>().AsNoTracking()
                .Where(x => x.ProductId == criteria.ProductId.Value)
                .Select(x => x.Id);
            downloads = downloads.Where(x => fileIds.Contains(x.ProductDownloadFileId));
        }

        var totalDownloads = await downloads.CountAsync(cancellationToken).ConfigureAwait(false);
        var successfulDownloads = await downloads.CountAsync(x => x.WasSuccessful, cancellationToken).ConfigureAwait(false);
        var failedDownloads = totalDownloads - successfulDownloads;

        var downloadDates = await downloads
            .Select(x => x.DownloadedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var timeSeries = BuildTimeSeriesFromDates(downloadDates, criteria, countOnly: true);

        return new DownloadsReportDto(
            criteria.FromUtc,
            criteria.ToUtc,
            totalDownloads,
            successfulDownloads,
            failedDownloads,
            timeSeries);
    }

    private async Task<ConversionReportDto> BuildConversionReportAsync(
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var carts = dbContext.Set<ShoppingCart>().AsNoTracking()
            .Where(x => x.CreatedAtUtc >= criteria.FromUtc && x.CreatedAtUtc <= criteria.ToUtc);

        if (criteria.StoreId.HasValue)
        {
            carts = carts.Where(x => x.StoreId == criteria.StoreId.Value);
        }

        if (criteria.CustomerId.HasValue)
        {
            carts = carts.Where(x => x.CustomerId == criteria.CustomerId.Value);
        }

        var checkouts = dbContext.Set<CheckoutSession>().AsNoTracking()
            .Where(x => x.CreatedAtUtc >= criteria.FromUtc && x.CreatedAtUtc <= criteria.ToUtc);

        if (criteria.StoreId.HasValue)
        {
            checkouts = checkouts.Where(x => x.StoreId == criteria.StoreId.Value);
        }

        if (criteria.CustomerId.HasValue)
        {
            checkouts = checkouts.Where(x => x.CustomerId == criteria.CustomerId.Value);
        }

        var orders = FilterOrders(criteria)
            .Where(x => x.Status == OrderStatus.Completed || x.PaymentStatus == OrderPaymentStatus.Paid);

        var cartsCreated = await carts.CountAsync(cancellationToken).ConfigureAwait(false);
        var checkoutsStarted = await checkouts.CountAsync(cancellationToken).ConfigureAwait(false);
        var ordersCompleted = await orders.CountAsync(cancellationToken).ConfigureAwait(false);

        var cartToCheckoutRate = CalculateRate(checkoutsStarted, cartsCreated);
        var checkoutToOrderRate = CalculateRate(ordersCompleted, checkoutsStarted);
        var cartToOrderRate = CalculateRate(ordersCompleted, cartsCreated);

        var orderDates = await orders.Select(x => x.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        var timeSeries = BuildTimeSeriesFromDates(orderDates, criteria, countOnly: true);

        return new ConversionReportDto(
            criteria.FromUtc,
            criteria.ToUtc,
            cartsCreated,
            checkoutsStarted,
            ordersCompleted,
            cartToCheckoutRate,
            checkoutToOrderRate,
            cartToOrderRate,
            timeSeries);
    }

    private IQueryable<Order> FilterOrders(AnalyticsFilterCriteria criteria)
    {
        var query = dbContext.Set<Order>().AsNoTracking()
            .Where(x => x.CreatedAtUtc >= criteria.FromUtc && x.CreatedAtUtc <= criteria.ToUtc);

        if (criteria.StoreId.HasValue)
        {
            query = query.Where(x => x.StoreId == criteria.StoreId.Value);
        }

        if (criteria.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == criteria.CustomerId.Value);
        }

        if (criteria.ProductId.HasValue)
        {
            query = query.Where(x => x.Items.Any(i => i.ProductId == criteria.ProductId.Value));
        }

        return query;
    }

    private IQueryable<Payment> FilterPayments(AnalyticsFilterCriteria criteria)
    {
        var query = dbContext.Set<Payment>().AsNoTracking()
            .Where(x => x.CreatedAtUtc >= criteria.FromUtc && x.CreatedAtUtc <= criteria.ToUtc);

        if (criteria.StoreId.HasValue)
        {
            query = query.Where(x => x.StoreId == criteria.StoreId.Value);
        }

        if (criteria.CustomerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == criteria.CustomerId.Value);
        }

        return query;
    }

    private async Task<IReadOnlyList<TopProductRowDto>> BuildTopProductsAsync(
        AnalyticsFilterCriteria criteria,
        bool sortByQuantity,
        CancellationToken cancellationToken)
    {
        var paidOrderIds = FilterOrders(criteria)
            .Where(x =>
                x.PaymentStatus == OrderPaymentStatus.Paid &&
                x.Status != OrderStatus.Cancelled)
            .Select(x => x.Id);

        var query = dbContext.Set<OrderItem>().AsNoTracking()
            .Where(item => paidOrderIds.Contains(item.OrderId));

        if (criteria.ProductId.HasValue)
        {
            query = query.Where(x => x.ProductId == criteria.ProductId.Value);
        }

        var rows = await query
            .GroupBy(x => new { x.ProductId, x.ProductName, x.CurrencyCode })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.CurrencyCode,
                QuantitySold = g.Sum(x => x.Quantity - x.CancelledQuantity),
                Revenue = g.Sum(x => x.LineTotal)
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ordered = sortByQuantity
            ? rows.OrderByDescending(x => x.QuantitySold).ThenByDescending(x => x.Revenue)
            : rows.OrderByDescending(x => x.Revenue).ThenByDescending(x => x.QuantitySold);

        return ordered
            .Take(criteria.TopProductsLimit)
            .Select(x => new TopProductRowDto(
                x.ProductId,
                x.ProductName,
                x.QuantitySold,
                x.Revenue,
                x.CurrencyCode))
            .ToList();
    }

    private async Task<IReadOnlyList<TimeSeriesPointDto>> BuildOrderRevenueTimeSeriesAsync(
        IQueryable<Order> orders,
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var rows = await orders
            .Select(x => new { x.CreatedAtUtc, x.GrandTotal })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return BuildTimeSeriesFromAmounts(rows.Select(x => (x.CreatedAtUtc, x.GrandTotal)).ToList(), criteria);
    }

    private async Task<IReadOnlyList<TimeSeriesPointDto>> BuildOrderCountTimeSeriesAsync(
        IQueryable<Order> orders,
        AnalyticsFilterCriteria criteria,
        CancellationToken cancellationToken)
    {
        var rows = await orders
            .Select(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return BuildTimeSeriesFromDates(rows, criteria, countOnly: true);
    }

    private static IReadOnlyList<TimeSeriesPointDto> BuildTimeSeriesFromAmounts(
        IReadOnlyList<(DateTime Timestamp, decimal Amount)> rows,
        AnalyticsFilterCriteria criteria)
    {
        var buckets = CreateMutableBuckets(criteria);
        foreach (var (timestamp, amount) in rows)
        {
            var bucket = FindMutableBucket(buckets, timestamp, criteria.Granularity);
            if (bucket is null)
            {
                continue;
            }

            bucket.Value += amount;
            bucket.Count++;
        }

        return buckets.Select(x => new TimeSeriesPointDto(x.PeriodStartUtc, x.Value, x.Count)).ToList();
    }

    private static IReadOnlyList<TimeSeriesPointDto> BuildTimeSeriesFromDates(
        IReadOnlyList<DateTime> rows,
        AnalyticsFilterCriteria criteria,
        bool countOnly)
    {
        var buckets = CreateMutableBuckets(criteria);
        foreach (var timestamp in rows)
        {
            var bucket = FindMutableBucket(buckets, timestamp, criteria.Granularity);
            if (bucket is null)
            {
                continue;
            }

            bucket.Count++;
            if (!countOnly)
            {
                bucket.Value++;
            }
        }

        return buckets.Select(x => new TimeSeriesPointDto(x.PeriodStartUtc, x.Value, x.Count)).ToList();
    }

    private static List<MutableTimeSeriesPoint> CreateMutableBuckets(AnalyticsFilterCriteria criteria)
    {
        var buckets = new List<MutableTimeSeriesPoint>();
        var cursor = TruncatePeriod(criteria.FromUtc, criteria.Granularity);
        var end = criteria.ToUtc;

        while (cursor <= end)
        {
            buckets.Add(new MutableTimeSeriesPoint { PeriodStartUtc = cursor });
            cursor = AdvancePeriod(cursor, criteria.Granularity);
        }

        return buckets;
    }

    private static MutableTimeSeriesPoint? FindMutableBucket(
        IList<MutableTimeSeriesPoint> buckets,
        DateTime timestamp,
        ReportGranularity granularity)
    {
        if (buckets.Count == 0)
        {
            return null;
        }

        var periodStart = TruncatePeriod(timestamp, granularity);
        for (var i = buckets.Count - 1; i >= 0; i--)
        {
            if (buckets[i].PeriodStartUtc <= periodStart)
            {
                return buckets[i];
            }
        }

        return null;
    }

    private static DateTime TruncatePeriod(DateTime value, ReportGranularity granularity) =>
        granularity switch
        {
            ReportGranularity.Week => value.Date.AddDays(-(int)value.DayOfWeek),
            ReportGranularity.Month => new DateTime(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => value.Date
        };

    private static DateTime AdvancePeriod(DateTime value, ReportGranularity granularity) =>
        granularity switch
        {
            ReportGranularity.Week => value.AddDays(7),
            ReportGranularity.Month => value.AddMonths(1),
            _ => value.AddDays(1)
        };

    private static decimal CalculateRate(int numerator, int denominator) =>
        denominator <= 0 ? 0m : Math.Round((decimal)numerator / denominator * 100m, 2);
}

internal sealed class MutableTimeSeriesPoint
{
    public DateTime PeriodStartUtc { get; init; }
    public decimal Value { get; set; }
    public int Count { get; set; }
}