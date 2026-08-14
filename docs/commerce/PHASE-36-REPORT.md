# PHASE 36 — Analytics / Reports / Admin Dashboard — Report

**Status:** Complete  
**Date:** 2026-08-13

---

## Summary

Phase 36 adds a read-only `Commerce.Analytics` module with reporting application services, an optimized EF read repository, admin API endpoints, CSV export, dashboard UI, and unit tests that validate report calculations against seeded data.

---

## Backend

### Module

| Project | Role |
|---|---|
| `Commerce.Analytics.Contracts` | DTOs, `IDashboardService`, `IReportsService`, filter query |
| `Commerce.Analytics.Application` | `DashboardService`, `ReportsService`, filter normalization |
| `Commerce.Analytics.Infrastructure` | `EfAnalyticsReadRepository`, permissions |
| `Commerce.Modules.Analytics` | Module registration |

### Reports delivered

- Revenue, Orders, Customers, Products, Inventory, Payments, Refunds, Discounts, Downloads, Conversion
- Dashboard summary aggregates KPIs + revenue time series + top products

### API endpoints

| Method | Route | Permission |
|---|---|---|
| GET | `/api/admin/dashboard` | `Analytics.View` |
| GET | `/api/admin/reports/revenue` | `Analytics.Reports.View` |
| GET | `/api/admin/reports/orders` | `Analytics.Reports.View` |
| GET | `/api/admin/reports/customers` | `Analytics.Reports.View` |
| GET | `/api/admin/reports/products` | `Analytics.Reports.View` |
| GET | `/api/admin/reports/inventory` | `Analytics.Reports.View` |
| GET | `/api/admin/reports/payments` | `Analytics.Reports.View` |
| GET | `/api/admin/reports/refunds` | `Analytics.Reports.View` |
| GET | `/api/admin/reports/discounts` | `Analytics.Reports.View` |
| GET | `/api/admin/reports/downloads` | `Analytics.Reports.View` |
| GET | `/api/admin/reports/conversion` | `Analytics.Reports.View` |
| GET | `/api/admin/reports/{ReportType}/export` | `Analytics.Reports.Export` |

Expensive aggregate queries live in `EfAnalyticsReadRepository` — transactional controllers are untouched.

---

## Frontend

- `AnalyticsApi` service + models in `@commerce/api`
- Admin dashboard page with date/store filters, KPI cards, top products table, revenue CSV export
- Route guarded with `Analytics.View`

---

## Tests

`Commerce.Tests.Unit.Analytics` — **5 tests**:

1. Revenue report sums paid non-cancelled orders only
2. Refunds report sums succeeded refunds in date range
3. Conversion report calculates funnel rates
4. Dashboard service returns aggregated summary
5. Reports service exports revenue CSV

---

## Files added

- `src/Commerce/Modules/Analytics/**`
- `src/Commerce/Host/Commerce.Host/Analytics/AdminAnalyticsController.cs`
- `tests/Commerce/Commerce.Tests.Unit.Analytics/**`
- `frontend/commerce-ui/libs/api/src/lib/analytics-api.service.ts`
- `frontend/commerce-ui/libs/api/src/lib/models/analytics.models.ts`
- `frontend/commerce-ui/apps/admin/src/app/pages/dashboard.page.ts` (replaced placeholder)

---

## Verification

```bash
dotnet build src/Commerce/Modules/Analytics/Commerce.Modules.Analytics/Commerce.Modules.Analytics.csproj
dotnet test tests/Commerce/Commerce.Tests.Unit.Analytics/Commerce.Tests.Unit.Analytics.csproj
```

---

## Next steps (not Phase 36)

- Wire `reports.generate` scheduling handler for async heavy exports
- Additional chart visualizations on dashboard
- Store picker integration (multi-store admin context)
