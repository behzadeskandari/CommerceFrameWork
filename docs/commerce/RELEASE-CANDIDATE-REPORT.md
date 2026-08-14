# Phase 49 — Production Release Candidate Report (V1.0)

**Date:** 2026-08-13  
**Phase:** 49 — Production Release Candidate / V1.0  
**Classification:** **RELEASE BLOCKED**

---

## Executive summary

Phase 49 focused on verification, stabilization, security review, release documentation, and deployment validation — not new features.

| Area | Result |
|------|--------|
| `Commerce.sln` Release build | **PASS** (0 errors) |
| `Commerce.Host` Release build | **PASS** |
| Host DI / startup validation | **FIXED during Phase 49** (was crash-on-start; now reaches hosting) |
| Backend unit/integration suites | **PARTIAL** (see Testing) |
| Angular admin + storefront production build | **PASS** |
| Angular headless tests | **PASS** (admin 4/4, storefront 3/3) |
| Smartstore import/reconciliation tests | **PASS** (14/14) |
| Docker clean install script | **NOT EXECUTED** (Docker available; full stack run deferred) |
| Smartstore parity (Phase 48 audit) | **Known gaps remain** (non-blocking for core loop, blocking for full migration parity) |

**Release decision:** **RELEASE BLOCKED**

Primary blockers:

1. **Financial/pricing unit regressions** — 9 `DiscountCalculationEngineTests` failures (test fixture `discountId` validation; fix applied, re-verification pending after host process lock cleared).
2. **Integration workflow suite** — failed before DI fixes; must be re-run after circular-dependency resolution.
3. **Architecture boundary tests** — 2 failures (Downloads module reference graph; Host concrete plugin references).

Do **not** tag V1.0 until integration workflows (registration → checkout → payment → order) pass on a clean install.

---

## Final architecture

Modular **.NET 10 monolith** with:

- **Host:** `Commerce.Host` — ASP.NET Core, 29 registered modules, plugin runtime, rate limiting, permission-based admin auth
- **Framework:** Core, Domain, Application, Data (EF Core + Identity), Infrastructure, Plugins, Scheduling, Search, SEO, Themes
- **Frontend:** Angular 19 dual app (`admin` + `storefront`) in `frontend/commerce-ui`
- **Deployment:** Docker Compose (SQL Server, Redis, Caddy, Commerce host) under `deploy/docker/`

```
Storefront/Admin (Angular)
        │
        ▼
  Commerce.Host (ASP.NET Core)
        │
   ┌────┴────┬──────────┬────────────┐
   │ Modules │ Plugins  │ Framework  │
   └────┬────┴──────────┴────────────┘
        ▼
   SQL Server + Redis (production)
```

---

## Modules (29 registered in host)

| Module | Purpose |
|--------|---------|
| Core | Installation, settings, module runtime |
| Customers | Accounts, groups, loyalty hooks |
| Catalog | Products, categories, offers |
| Media | Upload, storage keys, access control |
| Cart / Checkout | Basket, checkout orchestration |
| Pricing / Promotions | Discounts, coupons, rules |
| Inventory | Warehouses, stock |
| Shipping / Tax / Payments / Orders | Fulfillment & financial pipeline |
| Downloads | Digital entitlements, secure delivery |
| CMS / Search / Reviews / SEO | Content & discovery |
| Notifications / Scheduling | Email/SMS/in-app + background jobs |
| Themes / Store | Multi-store, currency, localization |
| Integration / Analytics / Audit / Observability | External API, metrics, audit trail |
| Cache / DisasterRecovery | Redis cache, backup/restore |
| SmartstoreImport | SQL import + reconciliation (Phases 46–47) |

---

## Plugins

Shipped under `src/Commerce/Plugins/`:

- Payment.Manual
- Search.Database
- Theme.Default
- (+ additional provider plugins per plugin discovery)

Plugin lifecycle (install → migrate → enable → configure → permission → controller → disable → uninstall) is implemented in `Commerce.Framework.Plugins` with package validation, manifest parsing, and development seeding.

**Phase 49 fix:** Plugin package validation uses archive entry count (manifest has no `Files` collection). Plugin development seeder uses scoped resolution via `IServiceScopeFactory`.

---

## Database

- **ORM:** EF Core 10, single `CommerceDbContext` with Identity
- **Migrations:** Module-owned `ICommerceMigration` registry, dependency-ordered
- **Multi-store:** Store-scoped entities and `IStoreContext`
- **Smartstore migration:** Import id maps in `ImportIdMapping`; reconciliation service with 15 check areas (unit-tested; live `scriptWithData.sql` not in repo)

---

## APIs

- **Admin:** `/api/admin/*` — permission attributes (`RequirePermission`), audit middleware, rate limit 300/min/IP
- **Storefront:** `/api/storefront/*` — catalog, cart, checkout, account, downloads
- **Installation:** `/installation` gate until configured
- **Health:** Mapped via `MapCommerceHealthChecks()`
- **Payments callback:** `/api/payments/callback/{provider}` with payload hash + optional callback key

---

## Admin & storefront

| Surface | Status |
|---------|--------|
| Admin Angular app | Production build **PASS**; 4 headless tests **PASS** |
| Storefront Angular app | Production build **PASS**; 3 headless tests **PASS** |
| Admin route coverage | 40+ route groups (Phase 48 audit: some API-only features lack admin UI) |

---

## Security verification

| Control | Verification |
|---------|--------------|
| Authentication | ASP.NET Identity; integration tests include auth/session (needs re-run) |
| Authorization | Permission policies + `AuditingPermissionAuthorizationHandler` |
| Rate limits | Global admin partition + named `auth` / `admin` limiters in `Program.cs` |
| CSRF | API-first; cookie auth uses SameSite patterns via Identity (standard ASP.NET) |
| XSS | Theme value sanitizer unit tests; CMS/content rendering uses encoded output patterns |
| SQL injection | EF Core parameterized queries; no raw SQL in module repositories reviewed |
| Download protection | Entitlement + guest token tests; storage key validation (`IsValidStorageKey`) |
| Payment verification | Manual provider + callback dispatcher; webhook hash header support |
| File upload | Media module storage key normalization (Phase 46) |
| Plugin package validation | Manifest + archive inspection in `PluginPackageService` |
| Secrets | `.env.example` for Docker; no secrets committed (verify before deploy) |

**Not measured:** Penetration test, OWASP ZAP scan, production TLS configuration (Caddy template exists under `deploy/docker`).

---

## Performance

**No production latency targets claimed** — limited local signals only:

| Signal | Observation |
|--------|-------------|
| Integration load test | `StorefrontCatalog_50ParallelRequests_MeetsLatencyBudget` **FAILED** (pre-DI-fix run; budget not validated) |
| DB indexes | Module EF configurations include indexes on hot paths (orders, catalog slugs); no full index audit run |
| Cache | Memory + optional Redis composite; output cache policies for catalog/search |
| Background jobs | Scheduling module with health probe (scoped resolution fix in Phase 49) |

---

## Testing

### Build

```
dotnet build Commerce.sln -c Release   → PASS (0 errors, 16 warnings)
```

### Backend verification (`scripts/test/run-verification.ps1`, Release)

| Project | Status | Notes |
|---------|--------|-------|
| Unit | **FAIL** | 255 pass, **10 fail** (pricing discounts, 1 review, 1 catalog) |
| Unit.Cache | PASS | 9/9 |
| Unit.Audit | PASS | 8/8 |
| Unit.Observability | PASS | 3/3 |
| Unit.Analytics | PASS | 5/5 |
| Unit.Deployment | PASS | 4/4 |
| Unit.DisasterRecovery | PASS | 4/4 |
| Unit.Integration | PASS | 7/7 |
| Unit.Notifications | PASS | 10/10 |
| Unit.PaymentProviders | PASS | 9/9 |
| Unit.PromotionsSeo | PASS | 14/14 |
| Unit.Scheduling | PASS | 9/9 |
| Unit.SmartstoreImport | PASS | **14/14** |
| Plugin.Sdk | PASS | 11/11 |
| Plugins | PASS | 11/11 |
| Architecture | **FAIL** | 64 pass, **2 fail** |
| Integration | **FAIL** | Host failed DI validation during run (fixed in Phase 49; **re-run required**) |

### Frontend

```
npm run build              → PASS (admin + storefront)
npm run test:admin         → PASS (4/4)
npm run test:storefront    → PASS (3/3)
```

### Workflow coverage (integration — pre-fix status)

| Workflow | Status |
|----------|--------|
| Customer registration → profile | **FAIL** (host DI) |
| Catalog / search / cart / checkout | **FAIL** (host DI) |
| Payment / order / fulfillment | **FAIL** (host DI) |
| Digital download entitlement | Dedicated test exists; **FAIL** (host DI) |
| Plugin lifecycle | **FAIL** (host DI) |
| Localization | **FAIL** (host DI) |

**Action:** Re-run `Commerce.Tests.Integration` after Phase 49 DI fixes.

---

## Migration (Smartstore)

| Area | Status |
|------|--------|
| SQL parser + conditional importers | IMPLEMENTED (16 importers) |
| Idempotency / ID mapping | IMPLEMENTED |
| Reconciliation (15 areas) | IMPLEMENTED — 14 unit tests |
| Live `scriptWithData.sql` import | **NOT VERIFIED** (file not in repo) |
| Known parity gaps | Manufacturers, bundles, partial attribute/download import — see `PHASE-48-FINAL-GAP-AUDIT.md` |

Scripts:

- `scripts/migration/run-smartstore-import.ps1`
- `scripts/migration/run-smartstore-reconciliation.ps1`

---

## Deployment

| Item | Status |
|------|--------|
| Docker Compose stack | Present (`deploy/docker/docker-compose.yml`) |
| Clean install script | `scripts/deploy/test-clean-install.ps1` — **not executed this phase** |
| Redis | Required for distributed cache lock when `Cache:Provider=Redis` |
| HTTPS | Caddy reverse proxy in compose template |
| Health checks | Observability module probes (scheduling/backup scoped fixes applied) |
| Backup / restore | DisasterRecovery module + unit tests PASS |
| Rollback | Documented pattern: redeploy previous image + DB restore from backup |

---

## Phase 49 stabilization fixes (summary)

Release-blocking compile and runtime issues addressed:

- Setting definition API drift (Shipping, Payments, Tax providers)
- `ShippingZone.LoadRules`, `Discount.LoadTargets` visibility for infrastructure/tests
- Host controller `ToActionResult` overload mismatches (Pricing, Inventory, DisasterRecovery, Payments)
- `Commerce.Host` missing SmartstoreImport module reference
- Plugin package validation + template content compile exclusion
- Test infrastructure: `tests/Directory.Build.props` (xUnit global usings), namespace collision fixes
- Notification/Scheduling test constructor updates (`ICorrelationContext`, `IBackgroundJobScheduler`)
- **Runtime DI:** circular dependencies broken via `IServiceScopeFactory` in `OrderPaidLoyaltyHandler`, `ShipmentAdminService`, `OrderFulfillmentSync`, health probes, `PluginDevelopmentSeeder`
- **Cache:** `DistributedCacheManager` only registered when Redis backend configured

---

## Known limitations (from Phase 48 + Phase 49)

1. **Manufacturers/brands** — MISSING  
2. **Grouped/bundle products** — PARTIAL  
3. **Smartstore import completeness** — PARTIAL (downloads, attributes, locale strings)  
4. **Advanced search (Elasticsearch)** — PARTIAL (database search plugin only)  
5. **Admin UI gaps** — returns, shipments, bulk export API-only  
6. **Carrier/tax provider plugins** — PARTIAL (flat/manual defaults)  
7. **SMS notifications** — log-only provider  
8. **Architecture test drift** — module boundary rules need update or code fix  
9. **Pricing unit tests** — fixture update in progress (`DiscountTarget.Create(1, ...)`)  

Full matrix: [`PHASE-48-FINAL-GAP-AUDIT.md`](PHASE-48-FINAL-GAP-AUDIT.md)

---

## Backup & rollback

- **Backup:** `IBackupService` + scheduled retention (`DisasterRecovery` module); checksum + recovery test flows unit-tested  
- **Rollback:** Deploy previous container image; restore DB from latest verified backup; plugin state in `CommercePluginInstallation` table  
- **RTO/RPO:** Not benchmarked in this phase

---

## Release checklist (remaining before V1.0 tag)

- [ ] Re-run full `scripts/test/run-verification.ps1` — all projects PASS  
- [ ] Re-run `Commerce.Tests.Integration` — customer registration, checkout, payment, digital download, plugin lifecycle  
- [ ] Execute `scripts/deploy/test-clean-install.ps1` on clean Docker host  
- [ ] Verify HTTPS + secrets via `.env` (no defaults in production)  
- [ ] Run Smartstore import against representative `scriptWithData.sql` in staging  
- [ ] Resolve architecture test failures or document accepted boundaries  
- [ ] Confirm pricing/discount unit tests green (financial domain)

---

## Sign-off

| Role | Decision |
|------|----------|
| Phase 49 agent | **RELEASE BLOCKED** |
| Reason | Financial unit test failures + integration suite not validated post-DI-fix; core host was non-startable until Phase 49 fixes |

**Phase 49 complete.** No further feature phases started automatically.
