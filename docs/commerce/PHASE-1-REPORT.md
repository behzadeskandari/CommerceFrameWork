# Phase 1 Report — Commerce Foundation

**Date:** 2026-08-11  
**Status:** Complete  
**Solution:** `Commerce.sln` (separate from `GateWayFrameWork.sln`)

---

## 1. Implemented Projects

| # | Project | Path | Purpose |
|---|---|---|---|
| 1 | `Commerce.Framework.Core` | `src/Commerce/Framework/Commerce.Framework.Core/` | Result pattern, errors, entities, domain events |
| 2 | `Commerce.Framework.Domain` | `src/Commerce/Framework/Commerce.Framework.Domain/` | Value objects: `Money`, `Address`, `Currency` |
| 3 | `Commerce.Framework.Contracts` | `src/Commerce/Framework/Commerce.Framework.Contracts/` | `ICommerceModule`, `IStoreContext`, module descriptors |
| 4 | `Commerce.Framework.Application` | `src/Commerce/Framework/Commerce.Framework.Application/` | Validation abstractions |
| 5 | `Commerce.Framework.Infrastructure` | `src/Commerce/Framework/Commerce.Framework.Infrastructure/` | Options, DI, `IEmailSender` (logging no-op) |
| 6 | `Commerce.Framework.Data` | `src/Commerce/Framework/Commerce.Framework.Data/` | DbContext, migration engine, SQL Server provider |
| 7 | `Commerce.Tests.Unit` | `tests/Commerce/Commerce.Tests.Unit/` | Foundation unit tests (28 tests) |
| 8 | `Commerce.Tests.Architecture` | `tests/Commerce/Commerce.Tests.Architecture/` | Dependency rule tests (6 tests) |

**Shared configuration:** `src/Commerce/Directory.Build.props` sets `net10.0`, nullable, implicit usings for framework projects.

---

## 2. Dependency Graph

```
Commerce.Framework.Core                    (no dependencies)

Commerce.Framework.Domain                  (no dependencies)

Commerce.Framework.Contracts
    └── Core

Commerce.Framework.Application
    ├── Core
    ├── Domain
    └── Contracts

Commerce.Framework.Infrastructure
    ├── Core
    ├── Contracts
    └── Application

Commerce.Framework.Data
    ├── Core
    ├── Domain
    ├── Contracts
    ├── Application
    └── Infrastructure
```

**Rules enforced:**
- Core has zero upstream references
- Domain has no EF Core or ASP.NET dependencies
- Contracts has no Infrastructure or Data references
- Application has no Infrastructure or Data references
- No Commerce project references banking assemblies

---

## 3. Important Design Decisions

### Result pattern
- `Result` / `Result<T>` with factory methods `Success()` and `Failure(Error)`
- Failures always carry a structured `Error` (code, message, type, optional metadata)
- `ResultExtensions` provides `Map`, `MapAsync`, `Bind`, `Match`, `Ensure` without over-engineering

### Domain events
- `IDomainEvent`, `DomainEvent` base record with `EventId` and `OccurredOnUtc`
- `Entity<TId>` holds domain events; `AggregateRoot<TId>` exposes `RaiseDomainEvent()`
- `IDomainEventDispatcher` contract defined for future async handler wiring (Phase 2+)

### Money
- `Currency` value object validates ISO 4217 3-letter codes
- `Money` uses `decimal` with scale capped at 4 decimal places, banker's rounding
- Cross-currency operations throw `InvalidOperationException` (no conversion in Phase 1)
- Negative amounts rejected at creation; subtraction cannot produce negative results

### Address
- Immutable record with factory `Create()` validating required fields
- Optional: `StateProvince`, `Address2`, `PhoneNumber`
- No coupling to Customer or Order entities

### Module contracts
- `ICommerceModule` with `ModuleDescriptor` and `ModuleDependency` — minimal, ready for Phase 2+ module registration
- `IStoreContext` defined for future multi-store resolution (no implementation yet)
- `ICommerceSettings` implemented via `CommerceOptions` in Infrastructure

### Validation
- Simple `IValidator<T>` returning `ValidationResult` with `ValidationError` list
- No MediatR or FluentValidation dependency — modules can add their own validators later

### DbContext
- `CommerceDbContext` starts with empty business model; only `MigrationVersionInfo` mapped
- Design-time factory supports `appsettings.json` + environment variables
- SQL Server configured via `ICommerceDbContextConfigurator`

### Migration engine
- Custom `ICommerceMigration` independent of EF Core migrations history
- `MigrationRegistry` discovers migrations, validates duplicates, orders by module + semantic version
- `MigrationRunner` executes pending migrations with transactional boundaries on relational providers
- In-memory provider skips transactions (test-friendly)
- `CoreInitialMigration` (v1.0.0) registers baseline history entry; schema created via `EnsureCreatedAsync`

### Provider abstraction
- `CommerceDatabaseProvider` enum: `SqlServer` (implemented), `PostgreSql` (throws `NotSupportedException` in Phase 1)
- `CommerceDataOptions` with `Provider`, `ConnectionString`, `CommandTimeoutSeconds`
- PostgreSQL can be added later by extending `CommerceDbContextConfigurator` without domain changes

---

## 4. Database Strategy

| Aspect | Phase 1 decision |
|---|---|
| Primary provider | SQL Server via `Microsoft.EntityFrameworkCore.SqlServer` 10.0.0 |
| Future provider | PostgreSQL — enum + configurator hook ready; not implemented |
| Connection string | `Commerce:Database:ConnectionString` (never hardcoded in source) |
| Migration history | Custom `MigrationVersionInfo` table (not EF `__EFMigrationsHistory`) |
| Business tables | None — intentionally empty DbContext |
| Smartstore import | Not started; `scriptWithData.sql` not read |

**Example configuration (not committed with secrets):**

```json
{
  "Commerce": {
    "ApplicationName": "Commerce",
    "Environment": "Development",
    "BaseUrl": "https://localhost:5001",
    "Database": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=CommerceFramework;Trusted_Connection=True;TrustServerCertificate=True"
    }
  }
}
```

---

## 5. Testing

### Unit tests (28)

| Area | Tests | Purpose |
|---|---|---|
| Result | 5 | Success, failure, generic, metadata, Map on failure |
| Domain events | 3 | Add, clear, entity equality by Id |
| Money | 9 | CRUD ops, currency mismatch, negative, precision, multiply |
| Address | 3 | Valid creation, missing fields, equality |
| Validation | 3 | Success, failure, IValidator implementation |
| Migration registry | 3 | Ordering, duplicate version, duplicate name |
| Migration runner | 2 | Pending detection, idempotent execution |

### Architecture tests (6)

| Rule | Validated |
|---|---|
| Core isolation | No references to Domain, Contracts, Application, Infrastructure, Data |
| Domain isolation | No Infrastructure, Data, EF Core, ASP.NET |
| Contracts isolation | No Infrastructure, Data, EF Core |
| Application isolation | No Infrastructure, Data, EF Core |
| Banking isolation | No Gateway/Bank1/Bank2 references |
| Target framework | All Commerce assemblies target `net10.0` |

---

## 6. Validation

| Check | Result |
|---|---|
| Build | **PASS** — `dotnet build Commerce.sln --configuration Release` (0 errors, 0 warnings) |
| Unit Tests | **PASS** — 28/28 passed |
| Architecture Tests | **PASS** — 6/6 passed |
| Banking untouched | **PASS** — No banking projects exist in this workspace; zero banking files modified |
| Commerce → Banking refs | **PASS** — Architecture tests confirm no banking references |
| Smartstore migration | **NOT STARTED** |

---

## 7. Repository Discrepancies (Phase 0 vs Actual)

| Phase 0 assumption | Actual Phase 1 decision |
|---|---|
| Add commerce to `GateWayFrameWork.sln` | Created separate `Commerce.sln` per approved instruction |
| Banking projects co-located in workspace | Workspace contains Phase 0 docs + new Commerce projects only; GateWayFrameWork not cloned (storage policy) |
| `src/Commerce/` flat structure | Used `src/Commerce/Framework/` for framework projects per prompt |
| Test projects inherit `Directory.Build.props` | Test projects declare `net10.0` explicitly (props only under `src/Commerce/`) |

---

## 8. Known Limitations (Intentionally Not Implemented)

- Catalog, Products, Categories, Customers, Orders, Cart, Checkout
- Payments, Shipping, Tax, Discounts, CMS, Search, Media
- Plugin engine, installation wizard, Commerce.Host
- ASP.NET Core Identity, permissions, store entities
- PostgreSQL provider implementation
- Smartstore data import (`scriptWithData.sql`)
- Redis caching, event bus implementation, scheduler
- Docker Compose for commerce (banking Docker unchanged)
- EF Core code-first migrations (using custom `ICommerceMigration` instead)
- Domain event dispatcher implementation
- Real email provider (logging no-op only)

---

## 9. File Layout Created

```
Commerce.sln
src/Commerce/
├── Directory.Build.props
└── Framework/
    ├── Commerce.Framework.Core/
    ├── Commerce.Framework.Domain/
    ├── Commerce.Framework.Contracts/
    ├── Commerce.Framework.Application/
    ├── Commerce.Framework.Infrastructure/
    └── Commerce.Framework.Data/
tests/Commerce/
├── Commerce.Tests.Unit/
└── Commerce.Tests.Architecture/
docs/commerce/
└── PHASE-1-REPORT.md
```

---

## 10. Next Phase

**PHASE 2 — Installation Engine**

- Create `Commerce.Host` with `/installation` wizard
- Wire migration runner at startup
- Seed engine, admin creation, store/language/currency defaults
- Installation state lockdown

Await explicit approval before starting Phase 2.
