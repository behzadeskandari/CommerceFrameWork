# Phase 50 — Report

**Date:** 2026-08-13  
**Classification:** **RELEASE CANDIDATE — NOT READY**

---

## Summary

Phase 50 repaired **unit tests**, **architecture tests**, **host startup DI**, and **plugin boundaries**. **Integration/E2E tests remain blocked** by a WebApplicationFactory hang (>90s) after host initialization; root cause under investigation (not file-lock related after clean environment).

---

## Problems discovered and root causes

| Issue | Root cause | Fix |
|-------|------------|-----|
| Unit: `DeleteProduct_SoftDeletesProduct` | `GetByIdAsync` uses `AsNoTracking`; `Update` conflicted with tracked entity from `Create` | `EfProductRepository.UpdateAsync` merges via `CurrentValues.SetValues` when tracked |
| Unit: `UpdateOwnAsync_ForbidsOtherCustomers` | Test stored review under wrong dictionary key before setting Id | Set `Id=42` before adding to fake repository |
| Architecture: Host plugin refs | Compile-time `ProjectReference` to Theme/Search plugins | Removed; added `ICommercePlugin` + `Plugin.json` + copy targets |
| Architecture: Downloads Media boundary | Test expected unused `Media.Contracts` ref; app uses local `IDownloadMediaResolver` | Updated test; removed unused csproj reference |
| Host startup DI crash | `PaymentProviderHealthProbe` singleton injected scoped `IPaymentProvider` | Use `IServiceScopeFactory` |
| Host hang with `SeedDevelopmentData` | All system plugins loaded in isolated ALC; optional plugins (Stripe/Test) included | Whitelist core dev plugins; default-context load for Manual/Theme/Search |
| Startup DB wait before install | `WaitForDatabaseSeconds` defaulted to 60s with empty connection string | Skip wait/migrations until installed; default wait = 0 |
| Background jobs before install | `BackgroundJobProcessor` queried DB before installation | Skip cycle when `!IsInstalledAsync` |

---

## Integration/E2E status

| Test | Result |
|------|--------|
| `InstallationFlowTests.CompleteInstallationFlow` | **HANG** (>90s, session timeout) |
| Full `Commerce.Tests.Integration` | **NOT COMPLETED** (hang) |

**Observed behavior:** Host reaches DataProtection initialization, then no further output until timeout. Occurs with and without plugin startup registration. **Not verified** as migration/API hang vs TestServer/environment issue.

**Classification:** **ENVIRONMENT / INFRASTRUCTURE — UNDER INVESTIGATION**

---

## Test results (verified)

| Suite | Result |
|-------|--------|
| `dotnet build Commerce.sln -c Release` | **PASS** (0 errors) |
| `Commerce.Tests.Unit` | **PASS** (265/265) |
| `Commerce.Tests.Architecture` | **PASS** (66/66) |
| `Commerce.Tests.Unit.SmartstoreImport` | **PASS** (14/14, prior run) |
| `Commerce.Tests.Integration` | **FAIL/HANG** |
| Angular production build | **PASS** |
| Admin headless tests | **PASS** (4/4) |
| Storefront headless tests | **PASS** (3/3) |
| `run-verification.ps1` | **BLOCKED** at Integration project (hang) |
| Docker clean-install | **NOT VERIFIED** (Docker present; script not executed end-to-end) |

---

## Security verification

| Check | Result |
|-------|--------|
| Download authorization unit tests | **PASS** (existing) |
| Integration security tests | **NOT RUN** (suite hang) |
| Payment callback/idempotency | **NOT RE-RUN E2E** |
| PaymentProviderHealthProbe DI | **FIXED** |

---

## Smartstore migration

| Item | Status |
|------|--------|
| `data/smartstore/scriptWithData.sql` | **Still absent** — release blocker for live migration parity |
| Unit tests with fixtures | **PASS** (14/14) |
| Reconciliation service | **Implemented** |

---

## Admin operational features (classification)

| Feature | Classification | Phase 50 action |
|---------|----------------|-----------------|
| Shipments | API/CLI sufficient | Documented in RUNNING-AND-USING |
| Returns/RMA | API/CLI sufficient | Documented |
| Audit | API/CLI sufficient | Documented |
| Analytics | API/CLI sufficient | Documented |
| Disaster Recovery | API/CLI sufficient | Documented |
| Webhooks | API/CLI sufficient | Documented |
| Product downloads admin | API/CLI sufficient | Documented |
| Smartstore import | Script + API | Documented |

No new Admin UI pages added (minimal scope).

---

## Files changed (code)

- `Commerce.Host.csproj` — removed plugin compile references
- `Commerce.Host/Program.cs` — removed direct plugin extension calls
- `Commerce.Plugin.Theme.Default/*` — `ICommercePlugin`, `Plugin.json`, copy target
- `Commerce.Plugin.Search.Database/*` — `ICommercePlugin`, `Plugin.json`, copy target
- `PluginDevelopmentSeeder.cs` / `EnabledPluginBootstrapper` — core plugin whitelist, default-context load
- `CommercePluginOptions.cs` — `RegisterServicesAtStartup`
- `PaymentProviderHealthProbe.cs` — scope factory
- `DeploymentStartupHostedService.cs` — skip DB wait when not installed
- `CommerceDeploymentOptions.cs` — default `WaitForDatabaseSeconds = 0`
- `BackgroundJobProcessor.cs` — skip when not installed
- `CatalogRepositories.cs` — EF tracking fix
- `ReviewApplicationTests.cs` — fixture fix
- `DownloadsArchitectureTests.cs` — boundary test update
- `Downloads.Application.csproj` — removed unused Media.Contracts ref
- `InstallationFlowTests.cs` — `RegisterServicesAtStartup=false` for tests
- `PluginAssemblyLoader.cs` — use `ExportedTypes`

---

## Remaining limitations

1. Integration/E2E suite must pass before production sign-off.
2. Live Smartstore SQL fixture required for migration verification.
3. Docker clean-install not executed in this phase.
4. Optional Admin UI for operational features deferred.

---

## Release Candidate decision

**RELEASE CANDIDATE — NOT READY**

Blockers: Integration tests, Smartstore SQL, E2E payment/download verification.
