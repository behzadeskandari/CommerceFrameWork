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

## 9. Approval Checkpoint

**PHASE 0 is complete.** Please review the six documents in `docs/commerce/` and approve before PHASE 1 begins.

Questions for approval:
1. Proceed with .NET 10 for commerce while banking stays on .NET 8?
2. SQL Server as primary DB with PostgreSQL compatibility?
3. Add commerce projects to existing `GateWayFrameWork.sln` or create separate `Commerce.sln`?
4. Place `scriptWithData.sql` at `data/smartstore/scriptWithData.sql`?
5. Any modules or plugins to prioritize differently?
