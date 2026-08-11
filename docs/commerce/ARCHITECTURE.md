# Commerce Framework — Architecture Analysis (PHASE 0)

**Status:** Analysis complete — no implementation changes yet  
**Date:** 2026-08-11  
**Source repository:** [GateWayFrameWork](https://github.com/behzadeskandari/GateWayFrameWork)  
**Target:** Modular commerce platform with Smartstore-comparable capabilities (independent implementation)

---

## 1. Executive Summary

This document records the PHASE 0 analysis of transforming **GateWayFrameWork** — a .NET 8 banking API gateway — into a production-grade **Commerce Framework** while preserving all existing banking functionality.

The strategy is **additive evolution**, not replacement:

| Capability | Current state | Target state |
|---|---|---|
| Banking gateway | Fully functional (27 projects, 64 tests) | Preserved unchanged |
| Commerce platform | Not present | Added as separate bounded capability |
| Deployment model | Gateway + independent bank services | Modular monolith commerce host + optional gateway coexistence |

The commerce engine will be a **modular monolith** — a single deployable ASP.NET Core application with strong module boundaries, extractable later if needed.

---

## 2. Existing GateWayFrameWork Analysis

### 2.1 Solution Overview

```
GateWayFrameWork.sln (27 projects)
├── Gateway.Framework.*     (9 framework modules)
├── Gateway.Host            (banking gateway host, port 5000)
├── plugins/                (Gateway.Bank.Bank1, Gateway.Bank.Bank2)
├── services/               (Bank1.Service.*, Bank2.Service.*, Audit.Abstractions)
└── tests/                  (Unit, Integration, Architecture, per-service tests)
```

**Stack:** .NET 8, YARP 2.2, Serilog, OpenTelemetry, JWT/OIDC, EF Core, xUnit, FluentAssertions

### 2.2 Framework Modules — What Exists Today

| Module | Responsibility | Commerce reuse potential |
|---|---|---|
| `Gateway.Framework.Core` | Abstractions, errors, API envelopes, idempotency, tenancy hooks | **High** — generalize to `Commerce.Framework.Core` |
| `Gateway.Framework.Shared` | DI helpers, HTTP/JSON extensions | **High** |
| `Gateway.Framework.Infrastructure` | Options, caching adapters (Memory/Redis) | **High** — wire caching in commerce host |
| `Gateway.Framework.Security` | JWT validation, policies, secure headers, IP allow-list | **High** — extend with commerce permissions |
| `Gateway.Framework.Logging` | Serilog, audit logging, sensitive data masking | **High** |
| `Gateway.Framework.Monitoring` | Health checks, OpenTelemetry | **High** |
| `Gateway.Framework.Resilience` | Banking-safe HttpClient retry/circuit breaker | **High** — payment/shipping provider calls |
| `Gateway.Framework.Gateway` | YARP, middleware pipeline, rate limiting, API versioning | **Medium** — optional for commerce API gateway |
| `Gateway.Framework.Plugins` | Compile-time plugin contract, YARP route merge, health | **Medium** — evolve to dynamic commerce plugin engine |

### 2.3 Key Abstractions Already Present

**Core cross-cutting:**
- `IClock`, `ICorrelationIdAccessor`, `ICurrentTenantAccessor`, `IFeatureManager`
- `IIdempotencyKeyAccessor`, `IdempotencyConstants`
- `ApiResponse<T>`, `BankingError`, `DomainException`, `ErrorCode`
- `TenantContext` (minimal multi-tenant hook)

**Plugin system (banking-specific):**
- `IBankingGatewayPlugin` — lifecycle: ConfigureServices, ConfigureRoutes, Initialize, Shutdown
- `IBankingGatewayPluginManager` — status, capabilities, routes, clusters
- `PluginRouteBuilder`, `PluginRouteRegistry`, `PluginProxyConfigProvider`
- `BankingPluginCapability` [Flags] — Accounts, Payment, Transfer, etc.

**Security:**
- JWT/OIDC validation-only (stateless, external IdP)
- Authorization policies: `AuthenticatedUser`, `RequiredScopes`, `BankingOperator`, `BankingAdmin`
- `ProductionConfigurationValidator` — fail-fast on unsafe config

**Observability:**
- Structured Serilog logging with request logging
- Correlation ID propagation (middleware → YARP → HttpClient)
- OpenTelemetry tracing/metrics with optional OTLP export
- Health: `/health/live`, `/health/ready`, plugin health aggregation

### 2.4 Host Startup Pipeline (Gateway.Host)

**DI registration order:**
1. Serilog → Core services → Infrastructure → Authentication
2. IP allow-list → Request size limit → Health checks → OpenTelemetry
3. Banking plugins (compile-time registration) → Gateway framework → Controllers

**Middleware order:**
```
ForwardedHeaders → HSTS → Serilog request logging → Secure headers
→ IP allow-list → Request size limit → Gateway middleware (exception, correlation, rate limit)
→ [Auth + Audit if enabled] → Map endpoints (health, controllers, YARP proxy)
→ ProductionConfigurationValidator
```

### 2.5 Bank Service Clean Architecture Pattern

Each bank service follows a proven template worth replicating for commerce modules:

```
Bank*.Service (host)
  → Application (handlers, validators)
    → Domain (entities, rules)
  → Infrastructure (EF Core, audit, external proxies)
  → Contracts (API DTOs)
```

**Isolation rules enforced by architecture tests:**
- Separate business DB per bounded context
- Separate audit DB per service
- No cross-bank project references
- Gateway never references bank business logic

### 2.6 Test Coverage (64 tests)

| Project | Coverage |
|---|---|
| `Gateway.Tests.Unit` | Plugin manager, audit, sensitive data masking |
| `Gateway.Tests.Integration` | JWT auth (11 scenarios), plugin routing, gateway→bank E2E |
| `Bank1.Service.Tests` | Account handlers, architecture rules, DB isolation, API integration |
| `Bank2.Service.Tests` | Payments, idempotency, architecture rules, DB isolation, API integration |

**Reusable test patterns:**
- `GatewayTestHostFactory`, `TestJwtIssuer`, `MultiHostRoutingHandler`
- Architecture dependency rule tests (NetArchTest-style)
- Database isolation tests

---

## 3. Smartstore Conceptual Architecture (Reference Only)

Smartstore 6.4 is analyzed **conceptually** — no source code is copied. Key architectural patterns observed:

| Pattern | Smartstore approach | Our independent approach |
|---|---|---|
| Modularity | `Smartstore.Core` with Catalog, Checkout, Content, Platform folders | `Commerce.Modules.*` per bounded context |
| Plugin system | Module assemblies with `Module.json`, Autofac modules, provider interfaces | `Plugin.json` manifest, dynamic discovery, DI registration |
| Migrations | Fluent Migrator + `MigrationVersionInfo` table | `ICommerceMigration` + EF Core migrations + version registry |
| Installation | `IsInstalled` flag, setup wizard, seed data | `/installation` wizard with 15 steps, idempotent |
| Data access | `SmartDbContext`, pooled factory, second-level cache | `CommerceDbContext`, module-specific configurations |
| Providers | `IPaymentMethod`, `IShippingRateComputationMethod`, `ITaxProvider` | `IPaymentProvider`, `IShippingProvider`, `ITaxProvider` |
| Multi-store | `IStoreContext`, `StoreMapping` | `IStoreContext`, `StoreMapping` (same concept, own implementation) |
| Localization | `LocaleStringResource`, `LocalizedProperty` | Same conceptual model, own services |
| Themes | Theme discovery, view overrides, `ThemeVariable` | Theme manifest, inheritance, widget zones |
| Events | Domain events + event consumers | `IEventBus` + `IEventHandler<T>` |

Smartstore's `CoreStarter` demonstrates early bootstrapping: type converters, DbContext factory, migration runner, modular DI registration — all concepts we will reimplement with our own abstractions.

---

## 4. Target Commerce Architecture

### 4.1 Dual-Capability Solution Model

The final solution coexists **banking** and **commerce** as separate bounded capabilities:

```
GateWayFrameWork.sln (evolved)
│
├── [PRESERVED] Banking Gateway
│   ├── Gateway.Framework.*
│   ├── Gateway.Host
│   ├── plugins/Gateway.Bank.*
│   └── services/Bank*.Service.*
│
├── [NEW] Commerce Framework
│   ├── Commerce.Host                    ← primary commerce entry point
│   ├── Commerce.Framework.*             ← shared platform (16 projects)
│   ├── Commerce.Modules.*               ← business modules (17 modules)
│   ├── Commerce.Plugins.*               ← dynamic plugins
│   ├── Commerce.Web                     ← storefront + admin MVC/Razor
│   └── Commerce.Tests.*                 ← unit, integration, architecture, E2E
│
└── [SHARED] Cross-cutting (optional bridge)
    └── Shared abstractions where genuinely common (audit, correlation, errors)
```

**Critical rule:** Commerce modules never reference banking projects. Banking gateway never references commerce modules. Shared code lives only in genuinely neutral abstractions.

### 4.2 Commerce Layer Model (Clean Architecture)

```
┌─────────────────────────────────────────────────────────┐
│  Commerce.Host / Commerce.Web                           │
│  (Controllers, Views, Middleware, API, Installation)    │
├─────────────────────────────────────────────────────────┤
│  Commerce.Modules.*.Application                         │
│  (Use cases, services, validators, DTOs)                │
├─────────────────────────────────────────────────────────┤
│  Commerce.Modules.*.Domain                              │
│  (Entities, value objects, domain events, rules)        │
├─────────────────────────────────────────────────────────┤
│  Commerce.Framework.Contracts                           │
│  (Cross-module interfaces: IPaymentProvider, etc.)      │
├─────────────────────────────────────────────────────────┤
│  Commerce.Framework.Infrastructure / Data               │
│  (EF Core, migrations, external integrations)           │
└─────────────────────────────────────────────────────────┘
```

### 4.3 Dependency Rules (Enforced by Architecture Tests)

| Rule | Rationale |
|---|---|
| Domain cannot depend on Web or Infrastructure | Clean Architecture |
| Core cannot depend on concrete plugins | Plugin isolation |
| Modules cannot depend on another module's Infrastructure | Module boundaries |
| Plugins cannot modify core source code | Extension without modification |
| Web depends on Application/Contracts only | Thin presentation layer |
| Infrastructure implements Application abstractions | Dependency inversion |

### 4.4 Request Flow — Storefront

```
Browser/API Client
  ↓
Commerce.Host
  ↓
Middleware (correlation, store resolution, localization, SEO slug routing)
  ↓
Commerce.Web Controllers / API
  ↓
Commerce.Modules.*.Application Services
  ↓
Commerce.Framework.Data (CommerceDbContext)
  ↓
SQL Server / PostgreSQL
```

### 4.5 Request Flow — Checkout Pipeline

```
Cart → Customer → Billing Address → Shipping Address
  → Shipping Method → Payment Method → Discounts → Taxes
  → Order Review → Order Creation → Payment → Confirmation
```

Each step is an `ICheckoutStep` registered via DI. Plugins participate through provider interfaces without modifying checkout core.

---

## 5. What Will Be Reused from GateWayFrameWork

### Tier 1 — Reuse directly (with namespace generalization)

| Asset | Adaptation |
|---|---|
| Error taxonomy (`DomainException`, `ErrorCode`, response envelopes) | Rename to commerce-neutral types in `Commerce.Framework.Core` |
| `ICorrelationIdAccessor`, `IClock`, `IFeatureManager` | Copy pattern to commerce core |
| Serilog + audit + sensitive data masking | Reuse logging module pattern |
| OpenTelemetry + health check infrastructure | Reuse monitoring module pattern |
| JWT validation + secure headers + IP allow-list | Extend for commerce admin/storefront auth |
| Banking-safe HttpClient resilience | Reuse for payment/shipping provider HTTP calls |
| `ProductionConfigurationValidator` pattern | Adapt for commerce production checks |
| Docker Compose multi-service topology | Extend for commerce + DB + Redis |
| Architecture test patterns | Replicate for commerce dependency rules |
| Bank service Clean Architecture template | Template for each commerce module |

### Tier 2 — Evolve significantly

| Asset | Evolution |
|---|---|
| `IBankingGatewayPlugin` | → `ICommercePlugin` with dynamic assembly loading |
| Compile-time plugin registration | → Runtime discovery from `Plugins/` directory |
| YARP route merge | → Commerce uses MVC routing + plugin route registration |
| `ICurrentTenantAccessor` | → Full `IStoreContext` with multi-store resolution |
| `ICache` adapters | → Wire into commerce with namespaced cache keys |

### Tier 3 — Do not reuse (banking-specific)

| Asset | Reason |
|---|---|
| `Gateway.Bank.Bank1/Bank2` plugins | Demo banking integrations |
| `Bank1/Bank2.Service.*` | Demo banking business logic |
| `BankingPluginCapability` enum | Banking-specific |
| YARP reverse proxy as primary routing | Commerce uses MVC + API, not proxy |
| Banking authorization policies | Replace with commerce permission system |

---

## 6. What Will Change

| Area | Change |
|---|---|
| Solution file | Add commerce projects alongside existing banking projects |
| Target framework | Upgrade new commerce projects to .NET 10; banking stays .NET 8 until separate upgrade |
| Primary database | Single commerce DB (SQL Server primary, PostgreSQL compatible) |
| Plugin model | From compile-time YARP plugins → dynamic runtime plugin engine |
| Authentication | From external JWT-only → ASP.NET Core Identity + permissions for commerce |
| Host application | New `Commerce.Host` separate from `Gateway.Host` |
| Documentation | New `docs/commerce/` section |

**What will NOT change:**
- Existing banking gateway projects, routes, tests, and Docker Compose banking services
- Banking plugin architecture and bank service Clean Architecture stacks
- Banking CI/CD compatibility

---

## 7. New Projects to Create

See [IMPLEMENTATION-ROADMAP.md](./IMPLEMENTATION-ROADMAP.md) for the complete solution tree. Summary:

**Framework (16 projects):** Core, Domain, Application, Contracts, Infrastructure, Data, Security, Logging, Caching, Events, Scheduling, Plugins, Media, Localization, Seo, Search, Themes, Cms

**Modules (17 modules):** Catalog, Customers, ShoppingCart, Checkout, Orders, Payments, Shipping, Tax, Discounts, Marketing, Cms, Media, Search, Localization, Seo, Administration, Stores

**Plugins (sample):** Payment.Manual, Payment.ZarinPal, Shipping.FlatRate, Tax.FixedRate, Search.Database, Storage.Local, Themes.Default

**Web + Host + Tests:** Commerce.Host, Commerce.Web, Commerce.Tests.{Unit,Integration,Architecture,EndToEnd}

---

## 8. Technology Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Runtime | .NET 10 for commerce | User requirement; banking stays .NET 8 |
| ORM | EF Core | Existing pattern in bank services; module configurations |
| Primary DB | SQL Server | Smartstore compatibility reference; user requirement |
| Secondary DB | PostgreSQL | Practical compatibility via provider abstraction |
| Identity | ASP.NET Core Identity | Commerce needs registration, login, roles — not just JWT validation |
| Logging | Serilog (structured) | Reuse from gateway framework |
| Observability | OpenTelemetry | Reuse from gateway framework |
| Testing | xUnit + FluentAssertions | Reuse from gateway framework |
| Caching | IMemoryCache + IDistributedCache (Redis) | Namespaced keys |
| Events | In-process event bus initially | Extractable to message bus later |
| Search | Database provider first | Plugin contract for Elasticsearch/OpenSearch |

---

## 9. Cross-Cutting Concerns Mapping

| Concern | Owner module | Key interfaces |
|---|---|---|
| Settings | `Commerce.Framework.Core` + Stores module | `ISettingService` |
| Permissions | Security + Administration | `IPermissionService` |
| Localization | `Commerce.Framework.Localization` | `ILocalizationService`, `ILanguageService` |
| SEO/URLs | `Commerce.Framework.Seo` | `IUrlService`, `ISlugService`, `ISeoService` |
| Media | `Commerce.Framework.Media` | `IMediaStorage`, `IMediaService` |
| Caching | `Commerce.Framework.Caching` | `ICacheManager` |
| Events | `Commerce.Framework.Events` | `IEventBus`, `IEventHandler<T>` |
| Scheduling | `Commerce.Framework.Scheduling` | `IScheduledTask`, `IScheduler` |
| Migrations | `Commerce.Framework.Data` | `ICommerceMigration` |

---

## 10. Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Scope explosion (20 phases) | Strict phase gates; each phase must compile + test before proceeding |
| Banking regression | Zero modifications to banking projects in early phases; separate CI job |
| Plugin isolation failure | Architecture tests + plugin table namespacing + manifest validation |
| Smartstore import complexity | Explicit import layer (Phase 17); never execute raw SQL |
| .NET 8 → .NET 10 split | Commerce on .NET 10; banking unchanged; shared abstractions as NuGet if needed |
| SQL file not yet in repo | DATABASE-MAP based on provided metadata; validate when `scriptWithData.sql` is available |

---

## 11. Analysis Limitations

- **Workspace was empty** at analysis time; repository analyzed via GitHub API/raw content
- **`scriptWithData.sql` not found** in workspace or GateWayFrameWork repo; database mapping uses provided bounded context classification and record counts from the implementation prompt
- **Smartstore analyzed conceptually** via public documentation and repository structure — no proprietary code copied

---

## 12. Implementation Status (updated PHASE 8)

Phases 0–8 are complete. See individual phase reports under `docs/commerce/`.

### PHASE 8 — Catalog 2.0 (complete)

The Catalog module was upgraded from Phase 4 foundation to a production-capable ecommerce catalog:

```text
Product → Offer → Price → (future Cart → Checkout → Order)
```

Key additions:

| Capability | Implementation |
|---|---|
| Product types | Simple, Variant (full); Digital (catalog); Grouped/Bundle (enum foundation) |
| Attributes | Definitions, options, assignments, localized via EntityTranslation |
| Variants | Stable IDs, attribute combinations, duplicate prevention, Cartesian generation |
| SKU | Global uniqueness across products and variants |
| Offers | Store-scoped + currency-explicit; replaces direct Product.Price for purchasing |
| Pricing | `IPricingService`, `ICatalogPricingReader`, `ResolvedPriceDto` snapshot |
| Storefront API | `/api/catalog/storefront/*` — published/visible/active only |
| Angular | Admin attributes/variants/offers; storefront variant selection + API pricing |

Catalog remains independent of Cart, Orders, Checkout, Payments, Shipping, and Inventory. Future modules consume `Commerce.Catalog.Contracts` only.

Full details: [PHASE-8-REPORT.md](./PHASE-8-REPORT.md)

---

### PHASE 9 — Media & File Storage (complete)

Added `Commerce.Media` module with `MediaAsset`, `IMediaStorage`, local file provider, secure storage keys, public/private delivery, thumbnails, and Catalog/Store media relationships.

Full details: [PHASE-9-REPORT.md](./PHASE-9-REPORT.md)

---

### PHASE 10 — Cart & Shopping Cart Engine (complete)

Added `Commerce.Cart` module with guest/customer carts, offer-based line items, server-side pricing, guest cookie token, cart merge on login, and storefront cart UI.

```text
Customer/Guest → Cart → CartItem → Offer → ResolvedPriceDto → Cart Totals → (future Checkout)
```

Key rules:
- Cart purchases **OfferId**, never Product directly
- Client sends only `offerId` + `quantity`; prices resolved via `ICatalogPricingReader`
- One active cart per Store + Customer/GuestToken + Currency
- Discount/shipping/tax totals = 0 with extension points for future modules

Full details: [PHASE-10-REPORT.md](./PHASE-10-REPORT.md)

---

### PHASE 11 — Checkout Engine (complete)

Added `Commerce.Checkout` module with checkout sessions, cart revalidation, price snapshots, address snapshots, provider abstractions (no-op defaults), guest/customer checkout, and storefront multi-step UI.

```text
Cart → Start Checkout → CheckoutSession → Addresses → Providers → Validate → ReadyForOrder → Order
```

Key rules:
- Checkout does **not** process payments
- Server-side totals only; price changes mark session `RequiresReview`
- One active checkout per cart; cart mutations invalidate stale checkout
- `ICheckoutOrderPreparationService` is the sole order-creation input boundary

Full details: [PHASE-11-REPORT.md](./PHASE-11-REPORT.md)

---

### PHASE 12 — Order Engine & Immutable Commercial Snapshots (complete)

Added `Commerce.Orders` module converting `ReadyForOrder` checkouts into immutable orders with full commercial snapshots.

```text
ReadyForOrder Checkout → ICheckoutOrderPreparationService → Order + OrderItems
  → Checkout Completed → Cart Converted
```

Key rules:
- Order stores immutable price, product, address, and customer contact snapshots
- Order numbering: `ORD-{year}-{sequence}` per store
- Idempotency via `Idempotency-Key` header + unique `CheckoutId` constraint
- Atomic creation via `OrderCreationTransaction`
- Payment, shipping, and inventory **not** processed in Phase 12

Full details: [PHASE-12-REPORT.md](./PHASE-12-REPORT.md)

---

## 13. Next Step

**PHASE 13** (awaiting explicit approval)
