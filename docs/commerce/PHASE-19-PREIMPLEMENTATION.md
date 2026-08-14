# PHASE 19 — Production Plugin Extensibility — Pre-Implementation

**Status:** Pre-implementation audit complete  
**Date:** 2026-08-12

---

## 1. Objective

Make Commerce runtime plugins production-grade: MVC controllers, settings, permissions, migrations, localization, multi-store configuration, hardened packages, failure isolation, and real (not fake) Angular admin extensibility metadata.

## 2. Phase 18 Baseline Inspected

| Area | Finding |
|---|---|
| Plugin engine | `Commerce.Framework.PluginContracts`, `Commerce.Framework.Plugins` |
| Manual Payment | Runtime discovery from `Plugins/Payment.Manual/` — no Host reference |
| Angular regression | `RouterLink` error already fixed in Phase 18 session |
| MVC controllers | Not implemented — planned for Phase 19 |
| Settings/permissions | Core `ISettingService` / `IModulePermissionContributor` exist; plugin-specific contracts missing |
| Multi-store | Global install only — store-scoped config missing |
| Audit | No Commerce audit module yet — use structured lifecycle logging |

## 3. Design Decisions

### 3.1 MVC Integration

- Register plugin assemblies via `ApplicationPartManager` + `AssemblyPart`
- Route prefix: `/api/plugins/{systemName}/...`
- Block plugin controllers from overriding core `/api/*` routes
- Controllers discovered only from validated, enabled plugin assemblies in `PluginAssemblyRegistry`

### 3.2 Service Registration

- `RegisterEnabledPluginServices` runs **before** `Build()` — true DI registration
- Enable/disable affects availability and **future startup**; DI/controller changes require restart (documented honestly)

### 3.3 Settings

- `IPluginSettingDefinitionProvider` per plugin
- Aggregated into core `ISettingDefinitionProvider` with `ModuleSystemName = plugin SystemName`
- Secret settings (`IsSecret`) never returned via GET APIs

### 3.4 Permissions

- `IPluginPermissionContributor` per plugin
- Aggregated via `PluginDynamicPermissionContributor` into existing permission registry

### 3.5 Migrations

- Plugins implement `ICommerceMigration` with `Module = plugin SystemName`
- Run on install via `PluginMigrationRunner`
- Default uninstall: `KeepData`; optional `RemoveData` removes store configuration only

### 3.6 Multi-Store

- `CommercePluginInstallation` — global install/enable
- `CommercePluginStoreConfiguration` — per-store enable + JSON config

### 3.7 Angular UI

- **No arbitrary JS from ZIP packages**
- Server-driven metadata via `/api/admin/plugins/{systemName}/ui`
- Compiled Angular shell renders tabs/forms from API data
- Full dynamic third-party Angular pages remain out of scope

### 3.8 Security

- Plugins are trusted server-side code
- Package validation protects filesystem boundaries, not malicious code execution

## 4. New Projects

- `Commerce.Plugin.Test` — test-only reference plugin (controller, settings, permissions, migration, localization, failure simulation)

## 5. Out of Scope (Phase 19)

ZarinPal, Stripe, PayPal, CMS, themes, marketplace, external plugin store.
