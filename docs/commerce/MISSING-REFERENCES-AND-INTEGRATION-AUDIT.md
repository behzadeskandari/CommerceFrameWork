# Missing References & Integration Audit

**Date:** 2026-08-13  
**Purpose:** Explicit broken/missing chains between backend, database, API, frontend, plugins, tests, and configuration.

---

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Chain complete |
| ⚠️ | Partial / API-only |
| ❌ | Broken or missing |

---

## 1. Database Provider (SQL Server)

```
CommerceDatabaseProvider.SqlServer (enum)
  ↓ ✅
CommerceDbContextConfigurator.UseSqlServer()
  ↓ ✅
appsettings.json → Commerce:Database:Provider = "SqlServer"
  ↓ ✅
deploy/docker/docker-compose.yml → Commerce__Database__Provider=SqlServer
  ↓ ✅
InstallationService.ConfigureDatabaseAsync()
  ↓ ✅ (aliases MSSQL, SQLServer added Phase 49)
Persisted → App_Data/commerce.database.json
```

**PostgreSql:** enum value exists; configuration **throws NotSupportedException** — deferred by design.

---

## 2. Core Commerce Flow

### Customer registration

```
Storefront register page
  ↓ ✅ Angular service
POST /api/auth/register (AuthController)
  ↓ ⚠️ Integration test FAILED (Phase 49 — re-run required)
Customers module persistence
  ↓ ✅ Identity + Customer entities
```

### Catalog → Cart → Checkout → Payment → Order

```
Storefront catalog pages
  ↓ ✅ libs/api catalog clients
GET /api/storefront/catalog/*
  ↓ ✅ CatalogStorefrontController
CartController / CheckoutController / PaymentsController / OrdersController
  ↓ ✅ Application services wired
CommerceDbContext
  ↓ ⚠️ E2E integration tests not green at audit time
```

---

## 3. Digital Downloads

```
Product (Digital type)
  ↓ ✅ Catalog domain
AdminProductDownloadsController
  ↓ ⚠️ NO admin Angular route in app.routes.ts
CustomerDownloadService + entitlement
  ↓ ✅ Domain + unit tests
DownloadsController (storefront)
  ↓ ✅
Media storage (MediaDownloadStorage)
  ↓ ✅
DigitalProductWorkflowTests (Integration)
  ↓ ❌ Failed Phase 49 (host startup)
```

---

## 4. Plugin Lifecycle

```
Plugin ZIP upload
  ↓ ✅ AdminPluginsController + PluginPackageService
Manifest validation
  ↓ ✅ PluginManifestValidator
Install → Migrate → Enable
  ↓ ✅ EfPluginRepository + migrations
Runtime discovery
  ↓ ⚠️ Host ALSO compile-references Manual, Search.Database, Theme.Default
PluginArchitectureTests.Host_DoesNotReferenceConcreteProviderPlugins
  ↓ ❌ FAIL (references exist in Commerce.Host.csproj)
```

**Chain:**

```
Commerce.Host.csproj
  → ProjectReference Commerce.Plugin.Payment.Manual
  → ProjectReference Commerce.Plugin.Search.Database
  → ProjectReference Commerce.Plugin.Theme.Default
  ↓
Architecture test expects ZERO Commerce.Plugin.* references
  ↓ ❌ MISMATCH
```

---

## 5. Downloads Module Architecture

```
Downloads.Application
  ↓ references
Commerce.Catalog.Contracts (IProductReader)
Commerce.Media.Contracts
  ↓
DownloadsArchitectureTests.DownloadsApplication_ReferencesMediaContractsOnly
  ↓ ❌ FAIL — also references Catalog.Application transitively
```

---

## 6. Orders ↔ Shipping ↔ Payments (DI — fixed Phase 49)

**Before Phase 49 (BROKEN):**

```
OrderService → IShipmentAdminService → IOrderService  (cycle)
PaymentService → OrderPaidLoyaltyHandler → IOrderService → IPaymentService  (cycle)
  ↓ ❌ Host crash on startup
```

**After Phase 49 (REPAIRED):**

```
OrderPaidLoyaltyHandler → IServiceScopeFactory → resolves IOrderService at runtime
ShipmentAdminService / OrderFulfillmentSync → same pattern
SchedulingHealthProbe / BackupHealthProbe / PluginDevelopmentSeeder → IServiceScopeFactory
  ↓ ✅ Host reaches hosting (verified Phase 49)
Integration tests
  ↓ ⚠️ NOT RE-RUN in this audit
```

---

## 7. Admin API → Frontend Chains

| Backend | Route prefix | Angular admin | Status |
|---------|--------------|---------------|--------|
| Products | `/api/admin/products` | `catalog/products` | ✅ |
| Orders | `/api/admin/orders` | `orders` | ✅ |
| Returns/RMA | order lifecycle API | — | ⚠️ missing UI |
| Shipments | `/api/admin/shipping/shipments` | — | ⚠️ missing UI |
| Discounts | `/api/admin/discounts` | `pricing/discounts` | ✅ |
| Audit | `/api/admin/audit` | — | ⚠️ missing UI |
| Analytics | `/api/admin/analytics` | — | ⚠️ missing UI |
| Disaster recovery | `/api/admin/disaster-recovery` | — | ⚠️ missing UI |
| Webhooks | `/api/admin/integration/webhooks` | — | ⚠️ missing UI |
| Product downloads | `/api/admin/products/{id}/downloads` | — | ⚠️ missing UI |
| Plugins | `/api/admin/plugins` | `plugins` | ✅ |
| Smartstore import | module services | — | ⚠️ script/API only |

---

## 8. Configuration References

| Key | Defined | Used | Status |
|-----|---------|------|--------|
| `Commerce:Database:Provider` | appsettings.json | DbContext configurator | ✅ |
| `Commerce:Database:ConnectionString` | appsettings (empty) | Install wizard / .env | ✅ |
| `Commerce:Cache:Provider` | appsettings | Cache module | ✅ |
| `Commerce:Cache:RedisConnectionString` | appsettings | Redis when Provider=Redis | ✅ |
| `frontend environment.apiBaseUrl` | environment.ts | HTTP interceptors | ✅ |
| Swagger | — | — | ❌ not configured |

---

## 9. Test Project References

| Test | Tests | Status |
|------|-------|--------|
| `Commerce.Tests.Unit` | Core domain | ❌ 10 failures (pricing, review, catalog) |
| `Commerce.Tests.Unit.SmartstoreImport` | Import + reconciliation | ✅ 14/14 |
| `Commerce.Tests.Architecture` | Boundaries | ❌ 2 failures |
| `Commerce.Tests.Integration` | E2E workflows | ❌ failed (pre-DI-fix run) |
| `Commerce.Tests.Unit` linked NotificationTests | Excluded from Unit.csproj | ✅ fixed Phase 49 |

---

## 10. Documentation ↔ Code

| Document | Reference | Code reality |
|----------|-----------|--------------|
| IMPLEMENTATION-ROADMAP | `data/smartstore/scriptWithData.sql` TO BE ADDED | ❌ still absent |
| PHASE-45-REPORT | E2E passing | ❌ superseded — integration fails |
| RELEASE-CANDIDATE-REPORT | RELEASE BLOCKED | ✅ accurate |
| ENVIRONMENT-CONFIGURATION | SqlServer primary | ✅ matches code |

---

## 11. Smartstore Migration

```
data/smartstore/scriptWithData.sql
  ↓ ❌ NOT IN REPOSITORY (unchanged Phase 50)
run-smartstore-import.ps1
  ↓ ✅ exists
Commerce.Modules.SmartstoreImport
  ↓ ✅ registered in Host
Unit tests with fixtures
  ↓ ✅ 14 passing
Live migration verification
  ↓ ❌ NOT VERIFIABLE
```

---

## Phase 50 — Resolved chains

| Chain | Before | After |
|-------|--------|-------|
| Host → Theme.Default / Search.Database compile refs | ❌ | ✅ Runtime `ICommercePlugin` |
| Downloads.Application → Media.Infrastructure | ⚠️ test mismatch | ✅ Local abstractions; test updated |
| ProductService delete → EF tracking | ❌ | ✅ Repository merge fix |
| PaymentProviderHealthProbe DI | ❌ | ✅ IServiceScopeFactory |
| Plugin startup (dev) | Hang loading all plugins | ✅ Core whitelist + default context |

## Phase 50 — Still broken

| Chain | Status |
|-------|--------|
| Integration WebApplicationFactory → E2E flows | ❌ HANG (>90s) |
| Smartstore live SQL | ❌ Missing file |
| Admin UI → shipments/audit/DR/etc. | ⚠️ API-only (documented) |

1. Re-run integration tests; fix any remaining startup/DI issues.
2. Fix `DiscountCalculationEngineTests` fixture (`discountId >= 1`).
3. Resolve Host plugin reference strategy (remove csproj refs OR update architecture tests).
4. Add admin UI routes for API-only operational features OR document API-only workflow in run guide.
5. Add representative Smartstore SQL fixture for staging migration tests.
