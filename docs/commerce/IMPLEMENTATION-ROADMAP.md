# Commerce Framework — Implementation Roadmap (PHASE 0)

**Purpose:** Phase-by-phase implementation plan with proposed solution tree, deliverables, and acceptance criteria.

---

## 1. Proposed Final Solution Tree

```
GateWayFrameWork.sln                          (evolved — banking + commerce)
│
├── docs/
│   ├── architecture.md                       (existing — banking)
│   ├── plugin-development.md                 (existing — banking)
│   ├── bank-service-development.md           (existing — banking)
│   └── commerce/                             (NEW — this analysis)
│       ├── ARCHITECTURE.md
│       ├── DATABASE-MAP.md
│       ├── MODULE-MAP.md
│       ├── PLUGIN-ARCHITECTURE.md
│       ├── MIGRATION-PLAN.md
│       └── IMPLEMENTATION-ROADMAP.md
│
├── data/
│   └── smartstore/
│       └── scriptWithData.sql                (TO BE ADDED — import reference)
│
│── ── PRESERVED: Banking Gateway ── ──
│
├── Gateway.Framework.Core/
├── Gateway.Framework.Shared/
├── Gateway.Framework.Infrastructure/
├── Gateway.Framework.Security/
├── Gateway.Framework.Logging/
├── Gateway.Framework.Monitoring/
├── Gateway.Framework.Resilience/
├── Gateway.Framework.Gateway/
├── Gateway.Framework.Plugins/
├── Gateway.Host/
├── plugins/
│   ├── Bank1/Gateway.Bank.Bank1/
│   └── Bank2/Gateway.Bank.Bank2/
├── services/
│   ├── Bank1.Service/
│   ├── Bank1.Service.Application/
│   ├── Bank1.Service.Contracts/
│   ├── Bank1.Service.Domain/
│   ├── Bank1.Service.Infrastructure/
│   ├── Bank2.Service/
│   ├── Bank2.Service.Application/
│   ├── Bank2.Service.Contracts/
│   ├── Bank2.Service.Domain/
│   ├── Bank2.Service.Infrastructure/
│   └── Shared/Banking.Service.Audit.Abstractions/
├── Gateway.Tests.Unit/
├── Gateway.Tests.Integration/
├── services/Bank1.Service.Tests/
├── services/Bank2.Service.Tests/
│
│── ── NEW: Commerce Framework ── ──
│
├── src/
│   │
│   ├── Commerce.Host/                                    ← ASP.NET Core host
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   ├── Installation/                                 ← /installation wizard
│   │   │   ├── Controllers/
│   │   │   ├── Views/
│   │   │   └── Services/
│   │   └── Plugins/                                      ← runtime plugin directory
│   │       ├── Payment.Manual/
│   │       ├── Payment.ZarinPal/
│   │       └── ...
│   │
│   ├── Commerce.Framework.Core/                          ← Result, errors, entity base, config
│   │   ├── Results/
│   │   ├── Errors/
│   │   ├── Entities/
│   │   ├── Events/
│   │   ├── Configuration/
│   │   └── Abstractions/
│   │
│   ├── Commerce.Framework.Domain/                        ← Shared value objects
│   │   ├── Money.cs
│   │   ├── Address.cs
│   │   └── Email.cs
│   │
│   ├── Commerce.Framework.Contracts/                     ← Cross-module interfaces
│   │   ├── Payments/IPaymentProvider.cs
│   │   ├── Shipping/IShippingProvider.cs
│   │   ├── Tax/ITaxProvider.cs
│   │   ├── Search/ISearchProvider.cs
│   │   ├── Media/IMediaStorageProvider.cs
│   │   ├── Plugins/ICommercePlugin.cs
│   │   └── Modules/ICommerceModule.cs
│   │
│   ├── Commerce.Framework.Application/                   ← Shared app patterns
│   │   ├── Behaviors/
│   │   └── Validation/
│   │
│   ├── Commerce.Framework.Infrastructure/                ← External integrations
│   │   ├── Email/
│   │   ├── FileSystem/
│   │   └── Configuration/
│   │
│   ├── Commerce.Framework.Data/                          ← DbContext, migrations, configs
│   │   ├── CommerceDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── Core/
│   │   │   ├── Catalog/
│   │   │   ├── Customers/
│   │   │   └── ...
│   │   ├── Migrations/
│   │   │   ├── Core/
│   │   │   ├── Catalog/
│   │   │   └── ...
│   │   ├── MigrationRunner.cs
│   │   └── Seeders/
│   │
│   ├── Commerce.Framework.Security/                      ← Identity, permissions
│   │   ├── Identity/
│   │   ├── Permissions/
│   │   └── Middleware/
│   │
│   ├── Commerce.Framework.Logging/                       ← Serilog, audit
│   │   ├── Serilog/
│   │   ├── Audit/
│   │   └── Masking/
│   │
│   ├── Commerce.Framework.Caching/                       ← Cache manager
│   │   ├── MemoryCacheManager.cs
│   │   └── RedisCacheManager.cs
│   │
│   ├── Commerce.Framework.Events/                        ← Event bus
│   │   ├── EventBus.cs
│   │   └── EventHandlerRegistry.cs
│   │
│   ├── Commerce.Framework.Scheduling/                    ← Background tasks
│   │   ├── Scheduler.cs
│   │   └── TaskRunner.cs
│   │
│   ├── Commerce.Framework.Plugins/                       ← Plugin engine
│   │   ├── Discovery/
│   │   ├── Loading/
│   │   ├── Lifecycle/
│   │   ├── Registry/
│   │   └── Validation/
│   │
│   ├── Commerce.Framework.Media/                         ← Media abstraction
│   │   ├── IMediaStorage.cs
│   │   └── MediaPathBuilder.cs
│   │
│   ├── Commerce.Framework.Localization/                  ← i18n engine
│   │   ├── LocalizationService.cs
│   │   └── ResourceManager.cs
│   │
│   ├── Commerce.Framework.Seo/                           ← URL/SEO engine
│   │   ├── UrlService.cs
│   │   ├── SlugService.cs
│   │   └── SitemapService.cs
│   │
│   ├── Commerce.Framework.Search/                        ← Search abstraction
│   │   ├── ISearchEngine.cs
│   │   └── DatabaseSearchProvider.cs
│   │
│   ├── Commerce.Framework.Themes/                        ← Theme engine
│   │   ├── ThemeProvider.cs
│   │   ├── ThemeContext.cs
│   │   └── ViewLocationExpander.cs
│   │
│   ├── Commerce.Framework.Cms/                           ← Widget engine
│   │   ├── WidgetRegistry.cs
│   │   ├── WidgetZoneRegistry.cs
│   │   └── WidgetRenderer.cs
│   │
│   ├── Commerce.Modules/
│   │   │
│   │   ├── Catalog/
│   │   │   ├── Commerce.Modules.Catalog.Domain/
│   │   │   └── Commerce.Modules.Catalog.Application/
│   │   │
│   │   ├── Customers/
│   │   │   ├── Commerce.Modules.Customers.Domain/
│   │   │   └── Commerce.Modules.Customers.Application/
│   │   │
│   │   ├── ShoppingCart/
│   │   │   ├── Commerce.Modules.ShoppingCart.Domain/
│   │   │   └── Commerce.Modules.ShoppingCart.Application/
│   │   │
│   │   ├── Checkout/
│   │   │   └── Commerce.Modules.Checkout.Application/
│   │   │
│   │   ├── Orders/
│   │   │   ├── Commerce.Modules.Orders.Domain/
│   │   │   └── Commerce.Modules.Orders.Application/
│   │   │
│   │   ├── Payments/
│   │   │   └── Commerce.Modules.Payments.Application/
│   │   │
│   │   ├── Shipping/
│   │   │   ├── Commerce.Modules.Shipping.Domain/
│   │   │   └── Commerce.Modules.Shipping.Application/
│   │   │
│   │   ├── Tax/
│   │   │   ├── Commerce.Modules.Tax.Domain/
│   │   │   └── Commerce.Modules.Tax.Application/
│   │   │
│   │   ├── Discounts/
│   │   │   ├── Commerce.Modules.Discounts.Domain/
│   │   │   └── Commerce.Modules.Discounts.Application/
│   │   │
│   │   ├── Marketing/
│   │   │   └── Commerce.Modules.Marketing.Application/
│   │   │
│   │   ├── Cms/
│   │   │   ├── Commerce.Modules.Cms.Domain/
│   │   │   └── Commerce.Modules.Cms.Application/
│   │   │
│   │   ├── Media/
│   │   │   ├── Commerce.Modules.Media.Domain/
│   │   │   └── Commerce.Modules.Media.Application/
│   │   │
│   │   ├── Search/
│   │   │   └── Commerce.Modules.Search.Application/
│   │   │
│   │   ├── Localization/
│   │   │   ├── Commerce.Modules.Localization.Domain/
│   │   │   └── Commerce.Modules.Localization.Application/
│   │   │
│   │   ├── Seo/
│   │   │   ├── Commerce.Modules.Seo.Domain/
│   │   │   └── Commerce.Modules.Seo.Application/
│   │   │
│   │   ├── Administration/
│   │   │   └── Commerce.Modules.Administration.Application/
│   │   │
│   │   └── Stores/
│   │       ├── Commerce.Modules.Stores.Domain/
│   │       └── Commerce.Modules.Stores.Application/
│   │
│   ├── Commerce.Plugins/
│   │   ├── Payments/
│   │   │   ├── Commerce.Plugin.Payment.Manual/
│   │   │   ├── Commerce.Plugin.Payment.ZarinPal/
│   │   │   └── Commerce.Plugin.Payment.Stripe/          (future)
│   │   ├── Shipping/
│   │   │   ├── Commerce.Plugin.Shipping.FlatRate/
│   │   │   └── Commerce.Plugin.Shipping.ByWeight/
│   │   ├── Tax/
│   │   │   └── Commerce.Plugin.Tax.FixedRate/
│   │   ├── Search/
│   │   │   └── Commerce.Plugin.Search.Database/
│   │   ├── Storage/
│   │   │   ├── Commerce.Plugin.Storage.Local/
│   │   │   └── Commerce.Plugin.Storage.S3/               (future)
│   │   └── Themes/
│   │       └── Commerce.Plugin.Theme.Default/
│   │
│   ├── Commerce.Web/                                     ← Storefront + Admin UI
│   │   ├── Controllers/
│   │   ├── Areas/
│   │   │   └── Admin/
│   │   │       ├── Controllers/
│   │   │       └── Views/
│   │   ├── Views/
│   │   │   ├── Shared/
│   │   │   ├── Catalog/
│   │   │   ├── Cart/
│   │   │   └── Checkout/
│   │   ├── Components/                                     ← View components (widgets)
│   │   ├── TagHelpers/
│   │   ├── Middleware/
│   │   │   ├── StoreContextMiddleware.cs
│   │   │   ├── LocalizationMiddleware.cs
│   │   │   └── SlugRoutingMiddleware.cs
│   │   └── wwwroot/
│   │
│   └── Commerce.Tests/
│       ├── Commerce.Tests.Unit/
│       ├── Commerce.Tests.Integration/
│       ├── Commerce.Tests.Architecture/
│       └── Commerce.Tests.EndToEnd/
│
├── Themes/                                                 ← Theme packages
│   ├── Default/
│   │   ├── theme.json
│   │   ├── Views/
│   │   └── wwwroot/
│   └── MyStore/
│
├── docker-compose.commerce.yml                             ← Commerce-specific compose
├── docker-compose.yml                                      (existing — banking, preserved)
└── k8s/                                                    (existing — extend later)
```

### Project count estimate

| Category | Projects |
|---|---|
| Preserved banking | 27 |
| Commerce Framework | 18 |
| Commerce Modules | ~30 (17 modules × ~2 layers) |
| Commerce Plugins | ~8 (initial set) |
| Commerce Web + Host | 2 |
| Commerce Tests | 4 |
| **Total (full build)** | **~89** |

Phases 1-4 create ~25 projects. Remaining projects added incrementally per phase.

---

## 2. Phase Summary

| Phase | Name | Key deliverables | Est. projects added |
|---|---|---|---|
| **0** | Architecture Analysis | 6 documentation files (this phase) | 0 |
| **1** | Commerce Foundation | Core, Domain, Contracts, Application, Infrastructure, Data | 6 |
| **2** | Installation Engine | /installation wizard, migration runner, seed | +Host partial |
| **3** | Plugin Engine | Dynamic discovery, lifecycle, manifest | +Plugins framework |
| **4** | Store + Localization | Store, Language, Currency, Settings, URL, RTL | +4 modules |
| **5** | Catalog | Product, Category, Manufacturer, Attributes | +2 modules |
| **6** | Customers | Customer, Roles, Auth, Addresses | +2 modules |
| **7** | Shopping Cart | Cart, calculation, validation | +2 modules |
| **8** | Checkout + Orders | Pipeline, order state machine | +3 modules |
| **9** | Payment Plugins | Manual + ZarinPal | +2 plugins |
| **10** | Shipping + Tax | Rate engines, providers | +2 modules, +2 plugins |
| **11** | Discounts + Rules | Rule engine, discounts | +2 modules |
| **12** | CMS | Topics, menus, widgets | +2 modules |
| **13** | Theme Engine | Discovery, activation, RTL | +1 framework, +1 plugin |
| **14** | Media + Downloads | Storage, secure downloads | +2 modules, +1 plugin |
| **15** | Search | Database search, facets | +2 modules, +1 plugin |
| **16** | Admin | Full admin UI | +1 module, Web admin |
| **17** | Smartstore Import | Data importers, validation | Import projects |
| **18** | REST API | Versioned API endpoints | API controllers |
| **19** | Performance | Caching, Redis, query optimization | Infrastructure |
| **20** | Production | Docker, OTel, health, secrets | DevOps |

---

## 3. Phase Details and Acceptance Criteria

### PHASE 0 — Architecture Analysis ✅ (this document)

**Deliverables:**
- [x] `docs/commerce/ARCHITECTURE.md`
- [x] `docs/commerce/DATABASE-MAP.md`
- [x] `docs/commerce/MODULE-MAP.md`
- [x] `docs/commerce/PLUGIN-ARCHITECTURE.md`
- [x] `docs/commerce/MIGRATION-PLAN.md`
- [x] `docs/commerce/IMPLEMENTATION-ROADMAP.md`

**Status:** Complete — awaiting approval to proceed.

---

### PHASE 1 — Commerce Foundation

**Create:**
- `Commerce.Framework.Core` — Result, errors, entity base, domain events, auditing, configuration
- `Commerce.Framework.Domain` — Money, Address, Email value objects
- `Commerce.Framework.Contracts` — ICommerceModule, base interfaces
- `Commerce.Framework.Application` — Validation base, common patterns
- `Commerce.Framework.Infrastructure` — Configuration, email abstraction
- `Commerce.Framework.Data` — CommerceDbContext, ICommerceMigration, MigrationRunner, core configurations
- `Commerce.Tests.Unit` — Foundation unit tests
- `Commerce.Tests.Architecture` — Dependency rule tests

**Do NOT implement:** Catalog, customers, or any business module.

**Acceptance criteria:**
- [ ] Solution compiles with new projects added to `GateWayFrameWork.sln`
- [ ] Banking projects unaffected (all 64 tests still pass)
- [ ] `Result<T>` and error types functional with unit tests
- [ ] `Entity` base with Id, audit fields (CreatedOnUtc, UpdatedOnUtc)
- [ ] `ICommerceMigration` + `MigrationRunner` discovers and runs migrations
- [ ] `MigrationVersionInfo` table created by core migration
- [ ] `CommerceDbContext` connects to SQL Server and PostgreSQL
- [ ] Architecture tests enforce dependency rules
- [ ] No TODO placeholders in Phase 1 code

---

### PHASE 2 — Installation Engine

**Create:**
- `Commerce.Host` (minimal — installation only)
- `/installation` wizard (15 steps)
- Seed engine with default data
- Installation state management
- Admin creation, store creation, default language/currency

**Acceptance criteria:**
- [ ] `/installation` accessible when not installed
- [ ] Wizard completes end-to-end on fresh SQL Server database
- [ ] Idempotent: re-running wizard steps causes no errors
- [ ] `/installation` blocked after completion
- [ ] Admin user created with secure password hashing
- [ ] Default store, language (en-US + fa-IR), currency (IRR) seeded
- [ ] Integration test: full installation flow

---

### PHASE 3 — Plugin Engine

**Create:**
- `Commerce.Framework.Plugins` — discovery, loading, lifecycle, registry
- `Plugin.json` manifest parsing and validation
- Plugin install/enable/disable/uninstall
- Plugin migrations support
- Plugin settings, permissions, localization registration

**Acceptance criteria:**
- [ ] Plugin discovered from `Plugins/` directory at startup
- [ ] Manifest validation rejects invalid plugins
- [ ] Plugin lifecycle (install → enable → disable → uninstall) works
- [ ] Plugin migrations run through MigrationRunner
- [ ] Architecture tests: plugins cannot reference core implementation
- [ ] Integration test: install a test plugin end-to-end

---

### PHASE 4 — Store + Localization

**Implement:** Store, Language, Currency, LocalizedProperty, LocaleStringResource, Settings, URL routing, RTL support.

**Acceptance criteria:**
- [ ] Multi-store resolution via middleware
- [ ] Settings service (global + store-specific)
- [ ] Language switching with fallback
- [ ] Persian (fa-IR) RTL rendering verified
- [ ] URL slug routing for entities
- [ ] Unit + integration tests

---

### PHASE 5-20

See phase summary table above. Each phase follows the same pattern:
1. Implement module/framework code
2. Create migrations
3. Write unit + integration tests
4. Verify architecture tests pass
5. Verify banking tests still pass
6. Document configuration
7. No TODO placeholders

Detailed acceptance criteria for Phases 5-20 will be expanded when each phase begins.

---

## 4. Development Conventions

### Naming

| Element | Convention | Example |
|---|---|---|
| Framework projects | `Commerce.Framework.{Name}` | `Commerce.Framework.Core` |
| Module projects | `Commerce.Modules.{Module}.{Layer}` | `Commerce.Modules.Catalog.Domain` |
| Plugin projects | `Commerce.Plugin.{Category}.{Name}` | `Commerce.Plugin.Payment.ZarinPal` |
| Namespaces | Match project name | `Commerce.Modules.Catalog.Domain` |
| Interfaces | `I{Name}` | `IProductService` |
| Services | `{Name}Service` | `ProductService` |
| Migrations | `{Scope}{Description}Migration` | `CatalogInitialMigration` |
| Settings | `{Module}.{Key}` | `Catalog.ProductsPerPage` |
| Cache keys | `{module}:{entity}:{id}` | `catalog:product:42` |
| Plugin tables | `{PluginName}{Entity}` | `ZarinPalTransaction` |

### Git workflow

- Commerce work on feature branches (`feature/commerce-phase-{N}`)
- Banking tests must pass on every commit
- No force push to main
- Phase completion = PR with documentation update

### CI pipeline (recommended)

```yaml
jobs:
  banking-tests:
    # Existing 64 tests — must always pass
  commerce-unit-tests:
    # Per-phase unit tests
  commerce-integration-tests:
    # Testcontainers SQL Server + PostgreSQL
  commerce-architecture-tests:
    # NetArchTest dependency rules
```

---

## 5. Technology Upgrade Path

| Component | Banking (current) | Commerce (target) |
|---|---|---|
| .NET | 8 | 10 |
| EF Core | 8 | 10 |
| ASP.NET Core | 8 | 10 |
| SQL Server | — | Primary |
| PostgreSQL | Used by bank services | Secondary (compatibility) |
| Redis | Configured but not wired | Wired for caching |
| OpenTelemetry | Implemented | Extended for commerce |
| Docker | docker-compose.yml | docker-compose.commerce.yml |

Banking projects remain on .NET 8 until a separate upgrade initiative.

---

## 6. Risk Register

| # | Risk | Impact | Mitigation | Phase |
|---|---|---|---|---|
| R1 | Scope creep across 20 phases | High | Strict phase gates; no feature bleed | All |
| R2 | Banking regression | High | Zero banking changes; CI gate | All |
| R3 | SQL file unavailable | Medium | Bounded context map from prompt metadata; validate when available | 17 |
| R4 | .NET 8/10 split complexity | Medium | Separate projects; no shared binary deps initially | 1 |
| R5 | Plugin isolation failure | High | Architecture tests + assembly load context | 3 |
| R6 | Smartstore import fidelity | Medium | Explicit importers with count validation | 17 |
| R7 | Performance without caching | Medium | Phase 19 dedicated; basic memory cache in Phase 4 | 4, 19 |
| R8 | Workspace access (storage policy) | Low | GitHub API analysis; clone when policy resolved | 0 |

---

## 7. Immediate Next Steps (PHASE 1 — upon approval)

1. Clone/copy GateWayFrameWork into workspace (when storage policy allows)
2. Add commerce projects to solution (6 framework projects)
3. Implement `Result<T>`, error types, entity base in Core
4. Implement `CommerceDbContext` + `ICommerceMigration` + `MigrationRunner` in Data
5. Add `Commerce.Tests.Unit` and `Commerce.Tests.Architecture`
6. Verify: `dotnet build` succeeds, banking tests pass, commerce tests pass
7. Report Phase 1 completion

**Estimated Phase 1 scope:** ~6 projects, ~40-60 source files, ~20-30 tests.

---

## 8. Definition of Done (All Phases)

A phase is **DONE** only when:

- [ ] Code compiles
- [ ] All new tests pass
- [ ] All existing banking tests pass (64+)
- [ ] Database migrations work (up + idempotent re-run)
- [ ] Architecture dependency tests pass
- [ ] Integration tests pass (where applicable)
- [ ] Documentation updated
- [ ] Configuration documented in appsettings examples
- [ ] Error handling and logging present
- [ ] Security considerations addressed
- [ ] No TODO placeholders remain for the current phase

---

## 10. Implemented Phases Tracker (actual execution order)

| Phase | Module | Status | Report |
|---|---|---|---|
| 48 | Smartstore Feature-Gap Audit | ✅ Complete (audit) | [PHASE-48-FINAL-GAP-AUDIT.md](./PHASE-48-FINAL-GAP-AUDIT.md) |
| 47 | Smartstore Reconciliation | ✅ Complete | [PHASE-47-REPORT.md](./PHASE-47-REPORT.md) |
| 46 | Smartstore SQL Import | ✅ Complete | [PHASE-46-REPORT.md](./PHASE-46-REPORT.md) |
| 14 | Pricing / Discounts / Coupons | ✅ Complete | [PHASE-14-REPORT.md](./PHASE-14-REPORT.md) |
| 15 | Shipping Engine | ✅ Complete | [PHASE-15-REPORT.md](./PHASE-15-REPORT.md) |
| 16 | Tax Engine | ✅ Complete | [PHASE-16-REPORT.md](./PHASE-16-REPORT.md) |
| 17 | Payment Engine | ✅ Complete | [PHASE-17-REPORT.md](./PHASE-17-REPORT.md) |
| 18 | Dynamic Plugin Engine | ✅ Complete | [PHASE-18-REPORT.md](./PHASE-18-REPORT.md) |
| 19 | Production Plugin Extensibility | ✅ Complete | [PHASE-19-REPORT.md](./PHASE-19-REPORT.md) |
| 20 | Digital Products & Downloads | ✅ Complete | [PHASE-20-REPORT.md](./PHASE-20-REPORT.md) |
| 21 | Advanced Pricing, Currency & Tax | ✅ Complete | [PHASE-21-REPORT.md](./PHASE-21-REPORT.md) |
| 22 | CMS / Topics / Pages / Widgets | ✅ Complete | [PHASE-22-REPORT.md](./PHASE-22-REPORT.md) |
| 23 | Theme Engine + Storefront Layout | ✅ Complete | [PHASE-23-REPORT.md](./PHASE-23-REPORT.md) |
| 24 | Search Engine | ✅ Complete | [PHASE-24-REPORT.md](./PHASE-24-REPORT.md) |

**Phase 18 deliverables:**
- `Commerce.Framework.PluginContracts` + `Commerce.Framework.Plugins`
- Runtime discovery, loading, lifecycle, ZIP packages
- Manual Payment migrated to runtime plugin
- Plugin admin API + Angular UI
- `Commerce.Tests.Plugins` unit tests
- Permissions: Plugins.View/Manage/Install/Configure

**Phase 19 deliverables:**
- Dynamic MVC controller discovery (`/api/plugins/{systemName}/...`)
- Plugin settings, permissions, migrations, localization
- Multi-store `CommercePluginStoreConfiguration`
- Hardened ZIP package installation
- `Commerce.Plugin.Test` reference plugin
- Extended admin API + Angular plugin detail tabs

**Phase 20 deliverables:**
- `Commerce.Downloads.*` module (domain, application, infrastructure, contracts)
- `DigitalProductTypes` shared helper; checkout/shipping skip digital-only carts
- `IOrderPaidHandler` hook grants download entitlements on payment
- `IDownloadStorage` abstraction backed by `IMediaStorage`
- Admin + storefront download APIs and Angular UI
- Permissions: Downloads.View/Configure/Manage
- Unit tests for entitlements, authorization rules, storage key validation

**Phase 21 deliverables:**
- `IProductPricingPipeline` — tier + customer-group pricing before discounts
- `CustomerGroup`, `CustomerGroupPrice`, `OfferTierPrice` entities
- `MonetaryRounding` centralized rules
- Tax settings admin API + UI
- Order tax line breakdown in API
- Customer group admin UI
- Permissions: CustomerGroups.View/Manage

**Phase 22 deliverables:**
- `Commerce.Cms.*` module (pages, topics, widgets, menus)
- Localized content via `LanguageId` (EN/FA ready)
- Server-driven widget types + zones
- HTML sanitization + slug security
- Admin page/topic management + storefront `/pages/{slug}`
- Permissions: Cms.Pages/Topics/Menus/Widgets.View|Manage

**Phase 23 deliverables:**
- `Commerce.Framework.Themes` + `Commerce.Themes.*` module
- Default theme plugin (`Themes.Default`)
- Per-store theme assignment + sanitized branding settings
- Layout engine with CMS widget zone integration
- Storefront RTL/LTR + CSS variable runtime
- Admin theme list + configuration
- Permissions: Themes.View|Manage

**Phase 24 deliverables:**
- `Commerce.Framework.Search` + `Commerce.Search.*` module
- `Commerce.Plugin.Search.Database` default provider
- Product index with store/language scoping
- Filters, sorting, pagination, suggestions
- Index job queue + catalog change hooks
- Storefront search UI on `/products`
- Permissions: Search.View|Manage

**Phase 25 deliverables:**
- `Commerce.Reviews.*` module (reviews, ratings, wishlists)
- Product reviews with 1–5 rating, moderation (Pending/Approved/Rejected)
- Verified purchase badge via `IOrderPurchaseVerifier`
- Rating aggregation (average, count, distribution) from approved reviews only
- Customer wishlist per store with availability
- Admin review moderation + wishlist browse
- Storefront product reviews, wishlist, account wishlist page
- Permissions: Reviews.View|Manage

**Phase 26 deliverables:**
- `Commerce.Promotions.*` rule-based promotion engine (conditions → actions → price adjustment)
- Percentage/fixed/BuyXGetY/linked discount actions; combination rules (Exclusive/Stackable/SameGroupExclusive)
- Usage limits, coupon codes, store/customer/product/category restrictions
- Pricing integration via `IPromotionEvaluationService` + `CustomerGroupId` on discount eligibility
- `Commerce.Seo.*` module + `Commerce.Framework.Seo` (URL records, metadata, robots.txt, sitemap.xml)
- Admin promotion + SEO management UI
- Permissions: Promotions.View|Manage, Seo.View|Manage
- Tests: `Commerce.Tests.Unit.PromotionsSeo` (14 passing)

**Phase 27 deliverables:**
- `Commerce.Notifications.*` module (email, SMS, in-app channels via provider abstractions)
- Template model with store/language scoping, variable substitution, enabled state
- Event handlers for CustomerRegistered, OrderCreated, PaymentSucceeded/Failed, OrderCancelled, ShipmentCreated, RefundCreated, DownloadAvailable
- Delivery log with exponential backoff retry + hosted poller (Phase 28 extends jobs)
- Admin template/history UI + storefront in-app API
- Framework `ISmsSender` + logging stub
- Permissions: Notifications.View|Manage
- Tests: `Commerce.Tests.Unit.Notifications`

**Phase 28 deliverables:**
- `Commerce.Framework.Scheduling` + `Commerce.Scheduling.*` durable job infrastructure
- Immediate, delayed, scheduled, recurring jobs with retry, dead-letter, cancellation
- DB-backed processor with atomic claim, distributed locks, execution history
- Admin job/recurring schedule UI
- Integrated notification retry + search index workers; stub handlers for future tasks
- Permissions: Scheduling.View|Manage
- Tests: `Commerce.Tests.Unit.Scheduling`

**Phase 29 deliverables:**
- Multi-warehouse inventory: `Warehouse`, `StockLocation`, per-warehouse `InventoryItem` rows
- Quantities: on-hand, reserved, available, incoming; low-stock thresholds
- Transfers, adjustments, incoming receipt; sale conversion on order payment
- Multi-warehouse reservation allocator; concurrency-safe reserve with row locking
- Recurring job `inventory.reservations.expire` (5 min)
- Admin warehouse API/UI; extended inventory detail (incoming, threshold)
- Integration: cart/checkout validation, order reserve/release/convert
- Tests: domain transfer/incoming/sale/low-stock; integration transfer/incoming/concurrent/oversell
- Docs: `PHASE-29-PREIMPLEMENTATION.md`, `PHASE-29-REPORT.md`

**Phase 30 deliverables:**
- Shipment records with tracking, ship/deliver lifecycle, order fulfillment sync
- Plugin shipping providers: `Shipping.FlatRate`, `Shipping.Pickup` via `IShippingProvider`
- Complete rate types: weight, order-subtotal, quantity; free shipping threshold
- Pickup checkout flow (no shipping address when pickup selected)
- Resilient multi-provider calculation; admin providers/settings/shipments API
- Checkout: digital/physical/mixed preserved; `RequiresAddress` on options
- Tests: mixed cart, invalid address, pickup, provider failure, rate types, shipment domain
- Docs: `PHASE-30-PREIMPLEMENTATION.md`, `PHASE-30-REPORT.md`

**Phase 34 deliverables:**
- `Commerce.Framework.Events` — in-process event bus, domain event interceptor
- `Commerce.Integration.*` — webhooks, API clients, integration events, idempotency
- 11 integration event types bridged from Orders/Customers/Catalog/Inventory
- Webhook delivery with HMAC signatures, retry, dead-letter, delivery history
- External API key auth + scoped `/api/external/orders` read API
- Admin webhook/API client endpoints
- Tests: `Commerce.Tests.Unit.Integration` (7 passing)

**Phase 35 deliverables:**
- `Commerce.Plugin.Payment.ZarinPal` — redirect gateway, server-side verify, callback handler
- `Commerce.Plugin.Payment.Stripe` — Checkout Session, webhook signature verification, refund/capture/void
- `IPaymentProviderSettingsReader`, `IPaymentCallbackDispatcher`, callback headers support
- Dev payment method seeds for zarinpal/stripe (inactive by default)
- Tests: `Commerce.Tests.Unit.PaymentProviders` (9 passing)

**Phase 37 deliverables:**
- `Commerce.Audit.*` module — tamper-resistant append-only audit log with SHA-256 hash chain
- Cross-module `IAuditPublisher` hooks: auth, admin HTTP, orders, payments, customers, settings, plugins, authorization denied
- `AuditSanitizer` — masks passwords, secrets, tokens, payment-sensitive fields
- Security: rate limiting (`/api/admin` 300/min, auth 20/min), security headers middleware
- Admin API: list, verify-chain, apply retention (`Audit:Retention:RetentionDays`, default 365)
- Permissions: `Audit.View`, `Audit.VerifyChain`, `Audit.ManageRetention`, `Audit.Export`
- Tests: `Commerce.Tests.Unit.Audit` (8 passing)

**Next:** Phase 37 — complete. Phase 38 — complete. Phase 39 — complete. Phase 40 — complete. Phase 41 — complete. Phase 42 — deferred. Phase 43 — complete. Phase 44 — complete. Phase 45 — complete. Phase 46 — complete. Phase 47 — complete. **Phase 48 — complete (audit only).**

**Phase 48 deliverables:**
- Final Smartstore feature-gap audit against real codebase (`PHASE-48-FINAL-GAP-AUDIT.md`)
- 110 capability rows classified: Implemented / Partial / Missing / N/A / Better than Smartstore
- Proposed post-48 phases 49–58 documented; **no feature implementation**

**Phase 47 deliverables:**
- `ISmartstoreReconciliationService` — post-import validation against source SQL
- 15 automated check areas: counts, prices, relationships, localization, SEO, store data
- Discrepancy classification: Match, Missing, Duplicate, Transformed, Invalid, NotApplicable
- Every discrepancy includes explanation + remediation path
- Tests: 5 reconciliation workflow tests (14 total SmartstoreImport tests)
- Docs: `SMARTSTORE-RECONCILIATION.md`; script: `scripts/migration/run-smartstore-reconciliation.ps1`

**Phase 46 deliverables:**
- `Commerce.Modules.SmartstoreImport` — SQL parser, import orchestration, legacy ID mapping, issue reporting
- Schema discovery from supplied SQL (`CREATE TABLE` / `INSERT`); no guessed Smartstore schema
- 16 conditional entity importers (Language → … → Localization); warn-only for unsupported entities
- Idempotent re-import via `ImportIdMapping`; duplicate file hash guard
- Test fixtures + `Commerce.Tests.Unit.SmartstoreImport` (9 passing)
- Mapping docs: `SMARTSTORE-IMPORT-MAPPING.md`; script: `scripts/migration/run-smartstore-import.ps1`
- Data placeholder: `data/smartstore/README.md` (`scriptWithData.sql` not yet in repo)

**Phase 45 deliverables:**
- E2E workflow tests: critical commerce, digital download, plugin lifecycle (`Phase=45` trait)
- Security, concurrency, load verification integration tests
- `IntegrationWorkflowHelper` — shared workflow API helpers
- `scripts/test/run-verification.ps1` — executes all test projects, TRX + summary JSON
- Docs: `TEST-VERIFICATION.md`, `PHASE-45-REPORT.md` (executed results + documented failures)
- **Executed:** 71 unit tests passing across 10 projects; host build blockers prevent E2E execution (documented)

**Phase 38 deliverables:**
- `Commerce.Observability.*` module — correlation IDs, request IDs, structured logging, metrics, tracing
- Health endpoints: `/health/live`, `/health/ready`, `/health` with DB, cache, scheduling, plugin, module, payment-provider checks
- Correlation propagation: Request → Cart → Checkout → Payment → Order → Notification (+ background jobs, audit)
- `LogSanitizer` — no secrets in logs
- Operational guide: `docs/commerce/COMMERCE-OPERATIONS.md`
- Tests: `Commerce.Tests.Unit.Observability` (3 passing)

**Phase 39 deliverables:**
- `Commerce.Cache.*` module — memory + Redis cache abstraction, composite L1/L2, distributed locking
- Application cache: storefront catalog, search queries, configuration/settings
- Output cache on anonymous storefront catalog/search GET endpoints
- `CacheCatalogInvalidator` wired via `ICatalogChangeNotifier`; setting eviction on write
- `CacheGuard` denylist — never cache cart/checkout/payment/order/inventory state
- Query optimizations: `AsNoTracking` product reads, composite catalog index, batch attribute option load
- Tests: `Commerce.Tests.Unit.Cache` (9 passing); `CachePerformanceProfiler` before/after measurement

**Phase 40 deliverables:**
- `@commerce/ui` shared admin library — page shell, data table, filter bar, bulk actions, form fields, toasts
- Grouped navigation with all routes; store selector; mobile drawer; skip link
- Persian RTL / English LTR with persisted locale; expanded admin i18n keys
- Enhanced pagination and confirm dialog; admin theme tokens + accessibility
- Reference page upgrades: products (bulk/export/sort), orders (filters/export), settings (typed/search)
- Tests: `admin-list.util.spec.ts`

**Phase 41 deliverables:**
- `Commerce.Plugin.Contracts`, `Commerce.Plugin.Sdk`, `Commerce.Plugin.Testing`, `Commerce.Plugin.Template`, `Commerce.Plugin.Cli`
- CLI: `commerce plugin create/build/test/pack/validate`
- MSBuild targets (`Commerce.Plugin.Sdk.targets`) — output copy to host Plugins folder
- Restored `PluginPackageService` + shared manifest validation in `Commerce.Framework.PluginContracts.Manifest`
- Static ZIP validation/packing (no plugin code execution)
- Developer guide: `docs/commerce/PLUGIN-DEVELOPMENT.md`
- Tests: `Commerce.Tests.Plugin.Sdk` (9 passing)

**Phase 42 — Marketplace / Multi-Vendor (OPTIONAL — DEFERRED):**
- **Not implemented** — no current product requirement for multi-vendor marketplace
- Assessment: [PHASE-42-ASSESSMENT.md](./PHASE-42-ASSESSMENT.md)
- Existing model remains **multi-store** (operator-owned `StoreId` scope), not seller marketplace
- "Marketplace" references in Phase 18–19 docs refer to **plugin distribution**, not seller commerce
- Revisit only with explicit stakeholder approval and updated roadmap scope

**Phase 43 deliverables:**
- `Commerce.Modules.DisasterRecovery` — coordinated SQL/file backups, retention, verification, recovery testing
- Components: database, media, downloads integrity, configuration, plugins, manifest
- Validity rule: backups valid for recovery only after `RestoreTested`
- RPO 24h / RTO 4h documented defaults
- Admin API, scheduled jobs (`backup.create`, `backup.retention`), `/health/ready` backup check
- Runbook: `docs/commerce/DISASTER-RECOVERY.md`
- Tests: `Commerce.Tests.Unit.DisasterRecovery`

**Phase 44 deliverables:**
- `deploy/docker/` — Dockerfile, Compose (dev/staging/production), Caddy HTTPS reverse proxy
- SQL Server + Redis services, persistent media/backup volumes, restart policies
- `CommerceDeploymentOptions` + startup DB wait / migration when installed
- `appsettings.Staging.json`, `appsettings.Production.json` — JSON logging, Redis, no secrets in git
- Secrets via `deploy/docker/.env.example` only; `.env` gitignored
- Docs: `DEPLOYMENT.md`, `ENVIRONMENT-CONFIGURATION.md`, rollback procedure
- Clean install: `scripts/deploy/test-clean-install.ps1` / `.sh`
- Tests: `Commerce.Tests.Unit.Deployment`

---

## 9. Approval Checkpoint

**PHASE 0 is complete.** Please review the six documents in `docs/commerce/` and approve before PHASE 1 begins.

Questions for approval:
1. Proceed with .NET 10 for commerce while banking stays on .NET 8?
2. SQL Server as primary DB with PostgreSQL compatibility?
3. Add commerce projects to existing `GateWayFrameWork.sln` or create separate `Commerce.sln`?
4. Place `scriptWithData.sql` at `data/smartstore/scriptWithData.sql`?
5. Any modules or plugins to prioritize differently?

---

## Final Audit Status

**Audit date:** 2026-08-13  
**Repository commit:** `3c09d31394f04d78ba4a0472a8f129ab16635154` (branch `master`, working tree has uncommitted changes)  
**Audit documents:** [FINAL-COMPREHENSIVE-AUDIT.md](./FINAL-COMPREHENSIVE-AUDIT.md), [MISSING-REFERENCES-AND-INTEGRATION-AUDIT.md](./MISSING-REFERENCES-AND-INTEGRATION-AUDIT.md), [RUNNING-AND-USING-COMMERCE.md](./RUNNING-AND-USING-COMMERCE.md), [DEVELOPER-WORKFLOW.md](./DEVELOPER-WORKFLOW.md), [PRODUCTION-READINESS-AUDIT.md](./PRODUCTION-READINESS-AUDIT.md)

### Build status

| Component | Status |
|-----------|--------|
| Backend Release build (`dotnet build Commerce.sln -c Release`) | **PASS** (0 errors) |
| Angular production build (`npm run build`) | **PASS** |
| Angular headless tests | **PASS** (admin 4, storefront 3) |

### Test status (last verified Phase 49 / audit)

| Suite | Status |
|-------|--------|
| `Commerce.Tests.Unit.SmartstoreImport` | **PASS** (14/14) |
| `Commerce.Tests.Unit` | **FAIL** (~10 failures — pricing/discount, catalog, reviews) |
| `Commerce.Tests.Architecture` | **FAIL** (2 — Host plugin refs, Downloads boundaries) |
| `Commerce.Tests.Integration` | **FAIL** (E2E workflows — re-run required post Phase 49 DI fixes) |
| Phase 49 verification script (`scripts/test/run-verification.ps1`) | **PARTIAL** — 13/16 projects pass |

### Database provider

| Provider | Status |
|----------|--------|
| **SQL Server** | **IMPLEMENTED** — default in appsettings, Docker, `UseSqlServer`, installation wizard (aliases: SqlServer, MSSQL, SQLServer) |
| PostgreSQL | **DEFERRED** — enum exists; configuration throws `NotSupportedException` |

### Critical issues (summary)

1. Integration E2E suite not green — blocks production sign-off.
2. Unit test failures in pricing/discount engine and related catalog/review tests.
3. Architecture boundary violations (Host compile-references concrete plugins; Downloads.Application references).
4. `data/smartstore/scriptWithData.sql` still absent — live Smartstore migration not verifiable.
5. Admin UI gaps for operational APIs (shipments, audit, analytics, DR, webhooks, product downloads, Smartstore import).

### Remaining work before production

See [PRODUCTION-READINESS-AUDIT.md](./PRODUCTION-READINESS-AUDIT.md) and [RELEASE-CANDIDATE-REPORT.md](./RELEASE-CANDIDATE-REPORT.md).

**Overall production readiness: NOT READY**

---

## Phase 50 Status (2026-08-13)

| Criterion | Before | After Phase 50 |
|-----------|--------|----------------|
| Release build | PASS | **PASS** |
| Unit tests | FAIL | **PASS** |
| Architecture tests | FAIL | **PASS** |
| Integration tests | FAIL | **HANG** (not green) |
| Frontend | PASS | **PASS** |
| Docker clean install | Not verified | Not verified |
| Release Candidate | NOT READY | **NOT READY** |

See [PHASE-50-REPORT.md](./PHASE-50-REPORT.md).
