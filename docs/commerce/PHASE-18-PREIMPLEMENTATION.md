# PHASE 18 — Dynamic Plugin Engine — Pre-Implementation

**Date:** 2026-08-12

---

## 1. Existing State

### Module runtime (compile-time)
- `ICommerceModule` / `CommerceModuleBase` — 14 modules registered explicitly in `Program.cs`
- Lifecycle: Discover → Validate → Register → Initialize → Start
- `ModuleStartupHostedService` runs after installation gate

### Plugins today
- `Commerce.Plugin.Payment.Manual` — compile-time Host reference + `AddManualPaymentProvider()`
- No `Plugin.json`, no dynamic loading, no plugin persistence

### Provider pattern (ready for plugins)
- `IPaymentProvider` resolved via `PaymentProviderResolver` + `IEnumerable<IPaymentProvider>`
- Payments module does NOT reference Manual plugin (correct)

---

## 2. Architecture Decision: Modules vs Plugins

| | Modules | Plugins |
|---|---|---|
| Registration | Compile-time in Program.cs | Runtime from Plugins/ directory |
| Packaging | Part of Commerce.sln | Independent folder/ZIP |
| Lifecycle | Always present | Install/Enable/Disable/Uninstall |
| Failure | Required modules fail startup | Optional plugins log + continue |

**Do NOT convert existing modules to plugins.**

---

## 3. New Projects

```
src/Commerce/Framework/PluginContracts/Commerce.Framework.PluginContracts
src/Commerce/Framework/Plugins/Commerce.Framework.Plugins
tests/Commerce/Commerce.Tests.Plugins
```

---

## 4. Core Contracts

- `ICommercePlugin` — RegisterServices, InitializeAsync, StartAsync, StopAsync
- `PluginDescriptor` — metadata from manifest + runtime state
- `PluginManifest` — parsed Plugin.json
- `IPluginDiscoveryService` — filesystem scan
- `ICommercePluginManager` — lifecycle orchestration
- `IPluginPackageService` — ZIP validate/extract/install
- `IPluginLoadContext` — AssemblyLoadContext wrapper
- `IPluginUiContributor` — future admin/storefront metadata (infra only)

---

## 5. Plugin Manifest (Plugin.json)

Required: systemName, name, version, assembly, minimumCommerceVersion  
Optional: author, description, website, dependencies, isSystemPlugin, isRequired

Validation before any assembly load: JSON schema, semver, system name format, assembly file exists, no duplicates, dependency graph acyclic, Commerce version compatible.

---

## 6. Loading Strategy

- `CollectibleAssemblyLoadContext` per plugin for dependency isolation
- Shared framework/contracts assemblies loaded from default context (not duplicated)
- Plugin private deps in `dependencies/` subfolder
- **Security:** plugins are trusted server-side code — NOT sandboxed. Document clearly.

---

## 7. Persistence

Table `CommercePluginInstallations`:
- SystemName (unique), Version, InstalledVersion, IsInstalled, IsEnabled, Status, InstalledAt, UpdatedAt, LastError, Configuration

Filesystem = discovered; database = installed/enabled.

---

## 8. Startup Order

```
Framework → Modules (RegisterServices) → Data → Plugin Discovery → Plugin Validation
→ Dependency Resolution → Plugin RegisterServices (enabled only) → Build
→ Module Initialize/Start → Plugin Initialize/Start (hosted service, post-install gate)
```

---

## 9. Manual Payment Migration

1. Add `ManualPaymentPlugin : ICommercePlugin` + `Plugin.json`
2. Output to `Commerce.Host/Plugins/Payment.Manual/`
3. Remove Host csproj reference to Manual plugin
4. Remove `AddManualPaymentProvider()` from Program.cs
5. Auto-install Manual via development seeder or integration test install step

---

## 10. Admin API

`GET/POST /api/admin/plugins/*` — list, detail, install, enable, disable, uninstall, reload

Permissions: `Plugins.View`, `Plugins.Manage`, `Plugins.Install`, `Plugins.Configure`

---

## 11. Out of Scope

- ZarinPal, Stripe, marketplace
- Angular runtime compiler for plugins
- Full plugin controller hot-reload (document limitation: enable/disable may require restart for some DI services)
