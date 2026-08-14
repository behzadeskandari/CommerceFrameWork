using Commerce.Analytics.Application.Abstractions;
using Commerce.Analytics.Application.Dashboard;
using Commerce.Analytics.Application.Reports;
using Commerce.Analytics.Contracts;
using Commerce.Analytics.Infrastructure.DependencyInjection;
using Commerce.Cart.Domain.Entities;
using Commerce.Checkout.Domain.Entities;
using Commerce.Customers.Domain.Entities;
using Commerce.Framework.Data.Db;
using Commerce.Orders.Domain.Entities;
using Commerce.Orders.Domain.Enums;
using Commerce.Payments.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Commerce.Tests.Unit.Analytics;

public sealed class Phase36AnalyticsTests
{
    [Fact]
    public async Task RevenueReport_SumsPaidNonCancelledOrders()
    {
        await using var provider = AnalyticsTestComposition.BuildProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IAnalyticsReadRepository>();

        var fromUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        SeedPaidOrder(db, "ORD-PAID-1", 100m, fromUtc.AddDays(1));
        SeedPaidOrder(db, "ORD-PAID-2", 250m, fromUtc.AddDays(2), discountTotal: 25m, taxTotal: 10m, shippingTotal: 15m);
        SeedPaidOrder(db, "ORD-CANCEL", 999m, fromUtc.AddDays(3), cancelled: true);
        SeedPaidOrder(db, "ORD-PENDING", 500m, fromUtc.AddDays(4), paid: false);
        await db.SaveChangesAsync();

        var report = await repository.GetRevenueReportAsync(
            new AnalyticsFilterCriteria(1, fromUtc, toUtc, null, null, ReportGranularity.Day, 10));

        Assert.Equal(350m, report.GrossRevenue);
        Assert.Equal(25m, report.DiscountTotal);
        Assert.Equal(10m, report.TaxTotal);
        Assert.Equal(15m, report.ShippingTotal);
        Assert.Equal(2, report.PaidOrderCount);
    }

    [Fact]
    public async Task RefundsReport_SumsSucceededRefundsInRange()
    {
        await using var provider = AnalyticsTestComposition.BuildProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IAnalyticsReadRepository>();

        var fromUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2026, 2, 28, 23, 59, 59, DateTimeKind.Utc);
        var createdAt = fromUtc.AddDays(5);

        var payment = Payment.Create(1, 10, 1, "USD", 200m, "Payment.Manual");
        payment.MarkCaptured();
        SetCreatedAt(payment, createdAt);
        db.Set<Payment>().Add(payment);
        await db.SaveChangesAsync();

        var refund = payment.ApplyRefund(40m, "USD", "Return");
        refund.MarkSucceeded();
        SetCreatedAt(refund, createdAt);
        db.Set<Refund>().Add(refund);
        await db.SaveChangesAsync();

        var report = await repository.GetRefundsReportAsync(
            new AnalyticsFilterCriteria(null, fromUtc, toUtc, null, null, ReportGranularity.Day, 10));

        Assert.Equal(1, report.RefundCount);
        Assert.Equal(40m, report.RefundAmount);
        Assert.Equal(1, report.SucceededCount);
    }

    [Fact]
    public async Task ConversionReport_CalculatesFunnelRates()
    {
        await using var provider = AnalyticsTestComposition.BuildProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IAnalyticsReadRepository>();

        var fromUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc);
        var createdAt = fromUtc.AddDays(2);

        db.Set<ShoppingCart>().Add(CreateCart(1, createdAt));
        db.Set<ShoppingCart>().Add(CreateCart(2, createdAt));
        db.Set<ShoppingCart>().Add(CreateCart(3, createdAt));
        db.Set<ShoppingCart>().Add(CreateCart(4, createdAt));

        db.Set<CheckoutSession>().Add(CreateCheckout(1, 1, createdAt));
        db.Set<CheckoutSession>().Add(CreateCheckout(2, 2, createdAt));

        SeedPaidOrder(db, "ORD-CONV-1", 50m, createdAt);
        await db.SaveChangesAsync();

        var report = await repository.GetConversionReportAsync(
            new AnalyticsFilterCriteria(1, fromUtc, toUtc, null, null, ReportGranularity.Day, 10));

        Assert.Equal(4, report.CartsCreated);
        Assert.Equal(2, report.CheckoutsStarted);
        Assert.Equal(1, report.OrdersCompleted);
        Assert.Equal(50m, report.CartToCheckoutRate);
        Assert.Equal(50m, report.CheckoutToOrderRate);
        Assert.Equal(25m, report.CartToOrderRate);
    }

    [Fact]
    public async Task DashboardService_ReturnsAggregatedSummary()
    {
        await using var provider = AnalyticsTestComposition.BuildProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardService>();

        var fromUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc);

        SeedPaidOrder(db, "ORD-DASH-1", 120m, fromUtc.AddDays(1));
        var customer = Customer.Create("user-1", "a@test.com", "Ann", "A", active: true);
        SetCreatedAt(customer, fromUtc.AddDays(1));
        db.Set<Customer>().Add(customer);
        await db.SaveChangesAsync();

        var result = await dashboard.GetSummaryAsync(new ReportFilterQuery(
            StoreId: null,
            FromUtc: fromUtc,
            ToUtc: toUtc,
            TopProductsLimit: 5));

        Assert.True(result.IsSuccess);
        Assert.Equal(120m, result.Value!.TotalRevenue);
        Assert.Equal(1, result.Value.OrderCount);
        Assert.Equal(1, result.Value.NewCustomers);
    }

    [Fact]
    public async Task ReportsService_ExportRevenue_ReturnsCsvContent()
    {
        await using var provider = AnalyticsTestComposition.BuildProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var reports = scope.ServiceProvider.GetRequiredService<IReportsService>();

        var fromUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2026, 5, 31, 23, 59, 59, DateTimeKind.Utc);
        SeedPaidOrder(db, "ORD-EXP-1", 75m, fromUtc.AddDays(1));
        await db.SaveChangesAsync();

        var result = await reports.ExportReportAsync(
            ReportType.Revenue,
            new ReportFilterQuery(StoreId: 1, FromUtc: fromUtc, ToUtc: toUtc));

        Assert.True(result.IsSuccess);
        Assert.Equal("text/csv", result.Value!.ContentType);
        Assert.Contains("gross_revenue,75", result.Value.Content, StringComparison.Ordinal);
    }

    private static void SeedPaidOrder(
        CommerceDbContext db,
        string orderNumber,
        decimal grandTotal,
        DateTime createdAtUtc,
        decimal discountTotal = 0m,
        decimal taxTotal = 0m,
        decimal shippingTotal = 0m,
        bool cancelled = false,
        bool paid = true)
    {
        var subtotal = grandTotal + discountTotal - taxTotal - shippingTotal;
        var order = Order.CreateFromCheckout(
            orderNumber,
            1,
            1,
            1,
            1,
            null,
            "customer@test.com",
            "Customer",
            null,
            1,
            "USD",
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            subtotal,
            discountTotal,
            shippingTotal,
            taxTotal,
            grandTotal,
            [OrderItem.Create(0, 1, 1, 10, 5, null, "Product A", null, "SKU-A", 1, subtotal, subtotal, discountTotal, taxTotal, grandTotal, "USD")]);

        if (paid)
        {
            order.MarkPaymentPaid();
        }

        if (cancelled)
        {
            order.Cancel("Test cancel");
        }

        SetCreatedAt(order, createdAtUtc);
        db.Set<Order>().Add(order);
    }

    private static ShoppingCart CreateCart(int customerId, DateTime createdAtUtc)
    {
        var cart = ShoppingCart.CreateForCustomer(1, customerId, 1, "USD", DateTime.UtcNow.AddDays(7));
        SetCreatedAt(cart, createdAtUtc);
        return cart;
    }

    private static CheckoutSession CreateCheckout(int cartId, int customerId, DateTime createdAtUtc)
    {
        var item = CheckoutSessionItem.Create(0, 1, 10, 5, null, 1, 100m, 100m, "USD");
        var checkout = CheckoutSession.Create(
            1,
            cartId,
            customerId,
            null,
            1,
            "USD",
            false,
            createdAtUtc,
            DateTime.UtcNow.AddDays(1),
            [item]);
        SetCreatedAt(checkout, createdAtUtc);
        return checkout;
    }

    private static void SetCreatedAt(object entity, DateTime createdAtUtc)
    {
        var property = entity.GetType().GetProperty("CreatedAtUtc");
        property?.SetValue(entity, createdAtUtc);
    }
}
