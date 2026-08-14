# PHASE 36 — Analytics / Reports / Admin Dashboard — Pre-implementation

**Status:** Pre-implementation  
**Date:** 2026-08-13

---

## Scope

Read-only analytics module with dedicated application services and optimized EF projections. Transactional controllers remain unchanged.

### Reports

| Report | Primary sources | Key metrics |
|---|---|---|
| Revenue | Orders (paid, non-cancelled) | Gross/net revenue, discount/tax/shipping totals, time series |
| Orders | Orders | Counts by order/payment/fulfillment status |
| Customers | Customers | New registrations, active customers |
| Products | Products, OrderItems | Catalog counts, top products by revenue/qty |
| Inventory | InventoryItems | Low stock, out of stock, on-hand/reserved/available |
| Payments | Payments | Captured/authorized amounts, failures, by provider |
| Refunds | Refunds | Count, amount, success/failure |
| Discounts | Orders, CouponUsage, PromotionUsage | Order discount total, coupon/promotion usage |
| Downloads | DownloadHistoryEntry | Total/success/failed downloads |
| Conversion | ShoppingCart, CheckoutSession, Orders | Cart → checkout → order funnel rates |

### Filters

- `StoreId`, `FromUtc`, `ToUtc` (default last 30 days)
- `ProductId`, `CustomerId`
- `Granularity` (Day/Week/Month) for time series
- `TopProductsLimit` (1–50)

### Permissions

- `Analytics.View` — dashboard summary
- `Analytics.Reports.View` — individual reports
- `Analytics.Reports.Export` — CSV export

### API

- `GET /api/admin/dashboard`
- `GET /api/admin/reports/{revenue|orders|customers|products|inventory|payments|refunds|discounts|downloads|conversion}`
- `GET /api/admin/reports/{ReportType}/export`

---

## Module layout

```
Commerce.Analytics.Contracts
Commerce.Analytics.Application   → DashboardService, ReportsService
Commerce.Analytics.Infrastructure → EfAnalyticsReadRepository
Commerce.Modules.Analytics
```

Revenue timing uses **order `CreatedAtUtc`** with `PaymentStatus.Paid`. Payment capture timing available via payments report.

---

## Out of scope (Phase 36)

- Saved report definitions / scheduled report generation (stub `reports.generate` job unchanged)
- PDF export
- External analytics plugins (Google Analytics)
