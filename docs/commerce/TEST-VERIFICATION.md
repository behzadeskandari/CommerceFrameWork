# Commerce — Automated Test Verification (Phase 45)

Final automated verification suite for the Commerce platform.

## Run all verification

```powershell
.\scripts\test\run-verification.ps1
```

Results (TRX + summary JSON): `artifacts/test-results/phase-45/`

Integration/E2E only:

```powershell
dotnet test tests/Commerce/Commerce.Tests.Integration/Commerce.Tests.Integration.csproj --filter "Phase=45"
```

## Test layers

| Layer | Project(s) | Scope |
|-------|------------|--------|
| **Unit** | `Commerce.Tests.Unit.*` | Domain, application, module logic |
| **Integration (API)** | `Commerce.Tests.Integration` | HTTP API via `WebApplicationFactory` |
| **Architecture** | `Commerce.Tests.Architecture` | Layering, plugin boundaries |
| **Plugin SDK** | `Commerce.Tests.Plugin.Sdk`, `Commerce.Tests.Plugins` | SDK, packaging, engine |
| **E2E workflows** | `Commerce.Tests.Integration` `[Phase=45]` | End-to-end critical paths |
| **Security** | `SecurityVerificationTests` | Auth, installation lock, private media |
| **Concurrency** | `ConcurrencyVerificationTests` | Idempotency, parallel health |
| **Load** | `LoadVerificationTests` | Parallel catalog / health probes |

## Critical workflow (E2E)

`CriticalCommerceWorkflowTests.Register_Browse_Cart_Checkout_Payment_Order_Fulfillment_Notification`

Register → Browse → Product → Cart → Checkout → Payment → Order → Fulfillment → Notification

## Digital workflow (E2E)

`DigitalProductWorkflowTests.DigitalProduct_Cart_Payment_Order_DownloadEntitlement_Download`

## Plugin workflow (E2E)

`PluginLifecycleWorkflowTests.Install_Migration_Enable_Settings_Permission_Disable_Uninstall`

## Existing integration coverage (pre–Phase 45)

Cart, checkout, orders, payments, inventory, shipping, tax, pricing, catalog, customers, media, plugins, multi-store, installation.

## CI recommendation

1. Run `run-verification.ps1` on every release candidate
2. Block merge if any **Pass** project regresses
3. Track **Fail** projects in backlog until host build is green

See [PHASE-45-REPORT.md](./PHASE-45-REPORT.md) for latest execution results and documented failures.
