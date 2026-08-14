# PHASE 18 — Dynamic Plugin Engine — Report

**Status:** Complete (backend compile/test blocked by .NET 10 SDK in this environment)  
**Date:** 2026-08-12

---

## 1. Summary

Phase 18 delivers a **real runtime plugin engine** separate from the existing compile-time module runtime. Plugins are discovered from a configurable `Plugins/` directory via `Plugin.json` manifests, loaded with `CollectibleAssemblyLoadContext`, and managed through install/enable/disable/uninstall lifecycle with database persistence. Manual Payment was migrated from compile-time Host registration to runtime plugin discovery, proving `IPaymentProvider` dynamic registration.

**Security note:** Plugins are trusted server-side extensions — `AssemblyLoadContext` provides dependency isolation, not a security sandbox. Only install plugins from trusted sources.

---

## 2. Architecture: Modules vs Plugins

| | Modules | Plugins |
|---|---|---|
| Registration | Compile-time in `Program.cs` | Runtime from `Plugins/` folder |
| Packaging | Part of Commerce.sln | Independent folder or ZIP |
| Lifecycle | Always loaded | Install → Enable → Disable → Uninstall |
| Failure | Required modules fail startup | Optional plugins log + continue |

Both systems coexist; modules were **not** converted to plugins.

---

## 3. Projects Created

| Project | Purpose |
|---|---|
| `Commerce.Framework.PluginContracts` | `ICommercePlugin`, discovery/lifecycle/admin contracts |
| `Commerce.Framework.Plugins` | Discovery, validation, loading, lifecycle, packages, persistence |
| `Commerce.Tests.Plugins` | Unit tests (manifest, dependencies, packages) |

**Path:** `src/Commerce/Framework/PluginContracts/`, `src/Commerce/Framework/Plugins/`

---

## 4. Plugin Lifecycle

```text
Discover (filesystem scan)
  → Validate (manifest + assembly + semver + dependencies)
  → Load (AssemblyLoadContext)
  → Register (ICommercePlugin.RegisterServices for enabled plugins)
  → Install (DB + migrations + permissions)
  → Initialize → Start (PluginStartupHostedService, post-install gate)
  → Enable / Disable / Uninstall
```

Shutdown occurs in reverse order.

---

## 5. Plugin Manifest (Plugin.json)

```json
{
  "systemName": "Payment.Manual",
  "name": "Manual Payment",
  "version": "1.0.0",
  "assembly": "Commerce.Plugin.Payment.Manual.dll",
  "minimumCommerceVersion": "1.0.0",
  "author": "Behzad",
  "description": "Manual payment provider plugin.",
  "isSystemPlugin": true,
  "dependencies": []
}
```

Validation occurs **before** any plugin code executes.

---

## 6. Manual Payment Migration

- Added `ManualPaymentPlugin : ICommercePlugin` registering `IPaymentProvider`
- Post-build copies DLL + manifest to `Commerce.Host/Plugins/Payment.Manual/`
- **Removed** `AddManualPaymentProvider()` from `Program.cs`
- **Removed** Host project reference to Manual plugin assembly
- Payments core unchanged — still resolves `IEnumerable<IPaymentProvider>`

---

## 7. Database

Table `CommercePluginInstallation`:
- SystemName (unique), Version, InstalledVersion, IsInstalled, IsEnabled, Status, LastError, Configuration, timestamps

Migration: `PluginInitialMigration` via `ICommerceMigration` (Module = `Commerce.Plugins`)

---

## 8. Startup Integration

```csharp
builder.Services.AddCommercePlugins(configuration, environment);
builder.Services.AddCommerceModules(...);
builder.Services.AddCommerceData(configuration);
builder.Services.RegisterEnabledPluginServices(configuration, environment);
builder.Services.AddCommerceModuleRuntime();
builder.Services.AddCommercePluginRuntime();
// ...
app.UsePluginStaticFiles();
```

Plugin services register **before** `Build()` for enabled/seeded plugins.

---

## 9. Admin API

| Endpoint | Permission |
|---|---|
| `GET /api/admin/plugins` | `Plugins.View` |
| `GET /api/admin/plugins/{systemName}` | `Plugins.View` |
| `POST /api/admin/plugins/{systemName}/install` | `Plugins.Install` |
| `POST /api/admin/plugins/{systemName}/enable` | `Plugins.Manage` |
| `POST /api/admin/plugins/{systemName}/disable` | `Plugins.Manage` |
| `POST /api/admin/plugins/{systemName}/uninstall` | `Plugins.Manage` |
| `POST /api/admin/plugins/{systemName}/reload` | `Plugins.Manage` |
| `POST /api/admin/plugins/install-package` | `Plugins.Install` |

---

## 10. Package Support

- ZIP format with `Plugin.json`, DLL, optional `dependencies/`, `wwwroot/`
- `IPluginPackageService` validates and extracts with path traversal protection
- No online marketplace

---

## 11. Static Files

Plugins may serve assets from `Plugins/{plugin}/wwwroot/` at `/plugins/{systemName}/...` with path traversal protection.

---

## 12. Frontend Extensibility

- `IPluginUiContributor` contract defined (admin nav, settings metadata)
- Infrastructure only — no Angular runtime compiler in this phase

---

## 13. Angular Admin

- `/plugins` — plugin list with status, install package upload
- `/plugins/:systemName` — detail with install/enable/disable/uninstall/reload
- EN + FA localization

---

## 14. Tests

| Suite | Coverage |
|---|---|
| `Commerce.Tests.Plugins` | Manifest parse/validate, dependency resolution, ZIP path traversal |
| `PluginArchitectureTests` | Host no Manual ref, Framework.Plugins no plugin refs |
| `PluginFlowTests` | Integration discovery/install |

**Frontend:** `npm test` — **PASS** (4/4)  
**Frontend build:** `npm run build` — **PASS**

**Backend:** **BLOCKED** — SDK 8.0.302 vs net10.0

---

## 15. Configuration

```json
"Commerce:Plugins:RootPath": "Plugins",
"Commerce:Plugins:SeedDevelopmentData": "true"
```

Development seeder auto-installs and enables `Payment.Manual`.

---

## 16. Known Limitations

1. **Enable/disable may require restart** for some DI-registered services (documented)
2. **No plugin controller hot-reload** — MVC controller discovery limited
3. **No ZarinPal/Stripe** — Manual plugin only proves architecture
4. **Plugins are not sandboxed** — trusted code only
5. **Backend validation blocked** until .NET 10 SDK

---

## 17. Before Phase 19

- Install .NET 10 SDK and run full test suite
- End-to-end test: disable Manual → checkout payment methods empty → enable → restored
- Plugin store-scoped enablement UI
- Extract FlatRate shipping / Internal tax to optional plugins (optional refactor)
- Plugin event subscription wiring

---

**Phase 18 complete. Awaiting explicit approval before Phase 19.**
