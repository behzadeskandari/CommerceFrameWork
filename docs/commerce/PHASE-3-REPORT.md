# Phase 3 Report — Commerce Module Runtime

**Date:** 2026-08-11  
**Status:** Complete  
**Solution:** `Commerce.sln`

---

## 1. Module Architecture

Phase 3 introduces the **Commerce Module Runtime** — the engine that discovers, validates, orders, registers, initializes, and starts independent Commerce modules.

```
Commerce.Host (composition root)
    └── AddCommerceModules / AddCommerceModuleRuntime
            └── Commerce.Framework.Application (runtime)
                    ├── ModuleDependencyResolver
                    ├── CommerceModuleRegistry
                    ├── CommerceModuleManager
                    └── ModuleStartupHostedService
            └── Commerce.Framework.Contracts (abstractions)
            └── Commerce.Framework.Data (store context, module settings, migration/seeder ordering)
            └── Commerce.Modules.Core (platform module)
```

### Key abstractions

| Type | Purpose |
|---|---|
| `ICommerceModule` | Module contract: `Descriptor`, `RegisterServices`, `InitializeAsync`, `StartAsync` |
| `ModuleDescriptor` | Stable identity: `Id`, `SystemName`, `Name`, `Version`, `Description`, `Dependencies`, `IsRequired` |
| `ModuleDependency` | Dependency on another module with optional minimum version |
| `ModuleState` | Lifecycle state enum |
| `ICommerceModuleRegistry` | Read module metadata and runtime state |
| `ICommerceModuleManager` | Orchestrates discovery, validation, registration, init, start, stop |
| `ICommerceModuleContext` | Controlled init/start context (services, config, store, logging) |
| `IModuleSettings` | Module-specific settings abstraction (global + store-scoped) |
| `IStoreContext` / `IStoreContextAccessor` | Multi-store runtime foundation |

---

## 2. Module Lifecycle

```
Discovered → Validated → Registered → Initialized → Started
                              ↓              ↓
                           Failed         Failed
                              ↓
                          Disabled
```

| State | Meaning |
|---|---|
| `Discovered` | Module type registered via `AddCommerceModules` |
| `Validated` | Metadata and dependencies validated |
| `Registered` | Services registered in DI |
| `Initialized` | `InitializeAsync` completed |
| `Started` | `StartAsync` completed |
| `Failed` | Init or start failed (required modules fail startup) |
| `Disabled` | Excluded via `Commerce:Modules:Disabled` configuration |

Impossible transitions are guarded in `CommerceModuleManager` (e.g. cannot start from `Discovered`).

---

## 3. Dependency Resolution

`ModuleDependencyResolver` performs deterministic topological ordering (Kahn's algorithm).

**Detected errors (actionable messages, no stack traces in API):**

- Missing dependency
- Circular dependency
- Duplicate module Id or SystemName
- Invalid module metadata
- Incompatible dependency version (semver minimum)

**Example:**

```text
Module Commerce.Test.Missing requires Commerce.Test.NotInstalled,
but Commerce.Test.NotInstalled is not installed.
```

Modules can be disabled via configuration:

```json
"Commerce": {
  "Modules": {
    "Disabled": ["Commerce.Test.Beta"]
  }
}
```

---

## 4. Module Migrations

Existing `ICommerceMigration` infrastructure integrated with module dependency order:

- Modules register migrations in `RegisterServices` as `ICommerceMigration` singletons
- `MigrationRegistry` orders by module dependency index, then semver, then name
- `Core` module migrations always run first

Each migration declares ownership via `ICommerceMigration.Module` (SystemName).

---

## 5. Module Seeders

Existing `ICommerceSeeder` infrastructure integrated:

- `IModuleSeeder` extends `ICommerceSeeder` with `ModuleSystemName`
- `SeederRunner` orders seeders by module dependency, then seeder order, then name
- Framework seeders without module ownership treated as `Core`

---

## 6. Store Context

Multi-store foundation (not full Stores module):

| Component | Purpose |
|---|---|
| `StoreContext` | Scoped runtime store identity |
| `IStoreContextAccessor` | Sets current store |
| `StoreContextInitializerService` | Loads default active bootstrap store |
| `StoreContextInitializer` | Hosted service for installed startup |
| Post-install hook | Store context initialized when installation completes |

When no store exists: `HasStore = false` (no single-store assumption).

---

## 7. Module Settings

`IModuleSettings` abstraction with `ModuleSettingsService` implementation:

- Keys stored as `Module.{SystemName}.{Key}` in `Settings` table
- Supports global (`StoreId = 0`) and store-specific values
- Ready for future admin UI and multi-store overrides

---

## 8. Host Integration

`Commerce.Host/Program.cs`:

```csharp
builder.Services.AddCommerceModules(builder.Configuration, modules =>
{
    modules.AddModule<CoreModule>();
});
builder.Services.AddCommerceData(builder.Configuration);
builder.Services.AddCommerceModuleRuntime();
```

**Startup flow (when installed):**

1. Load configuration and persisted DB config
2. `StoreContextInitializer` resolves default store
3. `ModuleStartupHostedService` registers, initializes, and starts modules
4. After fresh installation, `InstallationService.CompleteInstallationAsync` also starts module runtime

**Diagnostics:** `GET /modules` returns module name, version, state, dependencies, startup duration, failure reason.

---

## 9. Module Runtime vs Plugin Runtime

| | Module Runtime (Phase 3) | Plugin Runtime (Future) |
|---|---|---|
| **Purpose** | Business capability vertical slices | Infrastructure/provider extensions |
| **Discovery** | Explicit compile-time registration | Dynamic `Plugins/` folder scanning |
| **Loading** | Referenced assemblies | `AssemblyLoadContext` isolation |
| **Examples** | Catalog, Customers, Orders | Payment, Shipping, Tax providers |
| **Status** | **Implemented** | **NOT implemented** |

---

## 10. Projects Added

| Project | Path |
|---|---|
| `Commerce.Modules.Core` | `src/Commerce/Modules/Commerce.Modules.Core/` |
| `Commerce.Modules.TestSupport` | `tests/Commerce/Commerce.Modules.TestSupport/` |

---

## 11. Tests

### Unit tests (44)

| Area | Tests |
|---|---|
| Phase 1–2 foundation | 32 |
| Module dependency resolver | 7 |
| Module manager lifecycle | 3 |
| Migration/seeder module ordering | 2 |

### Architecture tests (10)

- Phase 1–2 layer rules (7)
- Framework does not reference `Commerce.Modules.*` (1)
- Modules do not reference Host (1)
- Modules do not reference banking assemblies (1)

### Integration tests (3)

- Installation flow regression (1)
- Module runtime starts Core module after installation (1)
- Installation lock after module runtime (1)

---

## 12. Validation

| Check | Result |
|---|---|
| Build | **PASS** (0 errors, 0 warnings) |
| Unit Tests | **PASS** (44/44) |
| Architecture Tests | **PASS** (10/10) |
| Integration Tests | **PASS** (3/3) |
| Installation Regression | **PASS** |
| Module Runtime | **PASS** |

**Total: 57/57 tests pass**

---

## 13. Limitations

**NOT implemented in Phase 3:**

- Catalog, Products, Categories, Customers, Orders, Cart, Checkout
- Payments, Shipping, Tax, Inventory, Promotions, CMS, Search
- Plugin engine (dynamic DLL loading, `Plugin.json`, assembly isolation)
- Theme engine
- Admin UI for module enable/disable
- Hot-unloading of modules
- Smartstore data import

---

## 14. Next Phase

**Phase 4** — await explicit approval before proceeding.

Potential scope: first business modules (e.g. Stores, Localization) built on this runtime.
