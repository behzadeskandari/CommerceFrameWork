# Phase 45 — Automated Integration / E2E / Load / Security Testing (Pre-Implementation)

## Goal

Build and execute final automated verification across unit, integration, API, E2E, security, concurrency, and load layers.

## Workflows under test

1. **Critical:** Register → Browse → Product → Cart → Checkout → Payment → Order → Fulfillment → Notification
2. **Digital:** Product → Cart → Payment → Order → Download entitlement → Download
3. **Plugin:** Install → Migration → Enable → Settings → Permission → Controller → Disable → Uninstall

## Deliverables

- E2E workflow tests in `Commerce.Tests.Integration` (trait `Phase=45`)
- Shared `IntegrationWorkflowHelper`
- `scripts/test/run-verification.ps1` — executes all test projects, writes TRX + summary
- `docs/commerce/TEST-VERIFICATION.md`
- `PHASE-45-REPORT.md` — execution results and **documented failures**

## Rule

Tests must be **executed**, not only authored. Failures are documented honestly.
