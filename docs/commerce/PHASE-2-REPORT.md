# Phase 2 Report — Commerce Installation & Bootstrap Engine

**Date:** 2026-08-11  
**Status:** Complete  
**Solution:** `Commerce.sln`

---

## 1. Architecture

Phase 2 introduces the executable **Commerce Host** and a full **installation subsystem** that bootstraps a fresh Commerce database, runs framework migrations and seeders, and establishes the initial administrator, store, language, and currency.

```
Commerce.Host                          (composition root, ASP.NET Core)
    └── Framework
            ├── Contracts              (installation interfaces, DTOs, ICommerceSeeder, IPasswordHasher)
            ├── Application            (InstallationRequirementsEvaluator)
            ├── Infrastructure         (PasswordHasherService, SensitiveValueMasker)
            └── Data                   (InstallationService, entities, migrations, seeders)
```

### Commerce.Host

| Component | Purpose |
|---|---|
| `Program.cs` | Startup, DI composition, persisted DB config load |
| `InstallationController` | REST installation wizard endpoints |
| `InstallationGateMiddleware` | Blocks `/installation` when installed; blocks normal routes when not installed |

### Installation subsystem

| Layer | Components |
|---|---|
| **Contracts** | `IInstallationService`, `IInstallationStateService`, status/step enums, request DTOs, `RequirementCheckResult` |
| **Application** | `InstallationRequirementsEvaluator` — runtime, config, writable `App_Data` checks |
| **Infrastructure** | `PasswordHasherService` (ASP.NET Identity hasher), `SensitiveValueMasker` |
| **Data** | `InstallationService`, `InstallationStateService`, `FileInstallationConnectionProvider`, `DynamicCommerceDbContextConfigurator` |

### Database bootstrap

- Connection strings are persisted to `App_Data/commerce.database.json` (never committed to source control).
- `DynamicCommerceDbContextConfigurator` resolves SQL Server at install time; supports `__InMemory__` token for automated tests.
- PostgreSQL enum exists but is not implemented yet — installer rejects it.

### Migration engine

Reuses Phase 1 infrastructure:

- `ICommerceMigration` / `MigrationRegistry` / `MigrationRunner`
- `MigrationVersionInfo` history table
- Per-migration transaction boundaries on relational providers; skipped on InMemory

Phase 2 adds `CoreInitialMigration` (baseline schema via `EnsureCreated`).

### Seed engine

- `ICommerceSeeder` contract with `Order`, `Name`, `SeedAsync`
- `SeederRunner` executes registered seeders in order
- Built-in seeders: `InstallationMetadataSeeder`, `DefaultSettingsSeeder`
- Seeders are idempotent — safe to run more than once

---

## 2. Installation Flow

| Step | Endpoint | Action |
|---|---|---|
| 1. Requirements | `POST /installation/requirements` | Validate .NET runtime, app config, writable `App_Data` |
| 2. Database | `POST /installation/database` | Configure SQL Server connection, probe connectivity, persist config |
| 3. Migrations | `POST /installation/migrate` | Discover, order, and apply pending migrations |
| 4. Seed | `POST /installation/seed` | Run framework seeders |
| 5. Administrator | `POST /installation/admin` | Create bootstrap administrator with hashed password |
| 6. Store | `POST /installation/store` | Create initial bootstrap store |
| 7. Language | `POST /installation/language` | Configure default language |
| 8. Currency | `POST /installation/currency` | Configure primary currency (user-selected, not hardcoded) |
| 9. Finish | `POST /installation/complete` | Mark installation complete, lock installer |

Additional endpoints:

- `GET /installation` — returns current installation state; returns `409 Conflict` when locked
- `GET /` — redirects to `/installation` when not installed; returns status when installed

### Startup behavior

```
Program starts
    → Load configuration
    → Register framework services
    → Load persisted database config (if any)
    → InstallationGateMiddleware
        ├── Not installed → only /installation (and /) accessible
        └── Installed     → /installation locked (409)
```

Migrations are **not** run automatically on every startup. Installation explicitly controls initial schema creation.

---

## 3. Database

Tables created during Phase 2 (via EF Core `EnsureCreated`):

| Table | Purpose |
|---|---|
| `MigrationVersionInfo` | Applied migration version history |
| `CommerceInstallation` | Installation state (`InstallationId`, `Status`, `Version`, `InstalledAt`, etc.) |
| `Settings` | Framework key/value settings |
| `BootstrapStore` | Initial store bootstrap (multi-store ready) |
| `BootstrapLanguage` | Default language bootstrap |
| `BootstrapCurrency` | Primary currency bootstrap |
| `BootstrapAdministrator` | Initial admin bootstrap (hashed password) |

Bootstrap tables are intentionally prefixed and minimal — they will transition to full module entities in later phases.

---

## 4. Security

| Concern | Implementation |
|---|---|
| **Password handling** | `IPasswordHasher` using ASP.NET Identity `PasswordHasher`; passwords never stored in plaintext |
| **Connection-string protection** | Never logged; `SensitiveValueMasker.MaskConnectionString()` used in log output |
| **Installation locking** | `CommerceInstallation.Status = Installed` + middleware returns 409 on `/installation` |
| **Error masking** | Client receives structured errors without stack traces, credentials, or internal paths |
| **No arbitrary SQL** | Installer validates input server-side; no SQL execution from HTTP requests |
| **Reinstallation prevention** | `IInstallationStateService.IsInstallationLockedAsync()` guards all installation operations |

### Administrator bootstrap transition point

`BootstrapAdministrator` is a temporary bootstrap mechanism. Phase 3+ will introduce the full Identity/Customers module. The password hasher abstraction (`IPasswordHasher`) is compatible with future ASP.NET Identity integration.

---

## 5. Tests

### Unit tests (32)

| Area | Tests |
|---|---|
| **Core** | Result pattern (5), Domain events (3) |
| **Domain** | Money (9), Address (3) |
| **Application** | Validation (3) |
| **Data/Migrations** | Registry ordering, pending/applied, failure behavior (5) |
| **Installation** | Password hasher, requirements evaluator, full install flow, plaintext rejection (4) |

### Architecture tests (7)

- Core, Domain, Contracts, Application layer dependency rules
- Framework projects do not reference `Commerce.Host`
- No banking assembly references
- All Commerce projects target `net10.0`

### Integration tests (1)

- `InstallationFlowTests.CompleteInstallationFlow_LocksInstallerAfterFinish` — end-to-end wizard via `WebApplicationFactory`

---

## 6. Validation

| Check | Result |
|---|---|
| Build | **PASS** (0 errors, 0 warnings) |
| Unit Tests | **PASS** (32/32) |
| Architecture Tests | **PASS** (7/7) |
| Integration Tests | **PASS** (1/1) |
| Installation Flow | **PASS** (automated via integration test) |

Commands:

```bash
dotnet restore Commerce.sln
dotnet build Commerce.sln --configuration Release
dotnet test Commerce.sln --configuration Release
```

---

## 7. Projects Added in Phase 2

| Project | Path |
|---|---|
| `Commerce.Host` | `src/Commerce/Host/Commerce.Host/` |
| `Commerce.Tests.Integration` | `tests/Commerce/Commerce.Tests.Integration/` |

Phase 1 framework projects extended with installation contracts, services, entities, and seeders.

---

## 8. Limitations

The following are **not implemented** in Phase 2:

- Catalog, Products, Categories
- Customers, Orders, Cart, Checkout
- Payments, Shipping, Tax, Promotions
- CMS, Search, Inventory
- Plugin engine (payment, shipping, ZarinPal)
- Themes, Admin dashboard, Customer portal
- Smartstore data import
- PostgreSQL provider
- Full ASP.NET Identity / customer account system
- Automatic migrations on production startup

---

## 9. Banking Impact

**NONE** — no banking projects were created or modified.

---

## 10. Next Phase

**Phase 3** — await explicit approval before proceeding.

Potential Phase 3 scope (per roadmap): core module foundations (Stores module, Identity/Customers bootstrap transition, etc.).
