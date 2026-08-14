# Phase 47 — Smartstore Migration Validation & Reconciliation (Report)

**Status:** Complete

## Summary

Phase 47 adds automated post-import reconciliation: compares Smartstore source SQL against Commerce entities via legacy ID mappings, produces classified discrepancy reports, and requires explanation + remediation for every gap.

## Deliverables

### Service

| Component | Role |
|-----------|------|
| `ISmartstoreReconciliationService` | Public reconciliation API |
| `SmartstoreReconciliationService` | Orchestration, import run resolution |
| `SmartstoreReconciliationChecks` | 15 automated check areas |
| `ReconciliationMappingIndex` | Legacy ID lookup + duplicate detection |

### Classifications

`Match`, `Missing`, `Duplicate`, `Transformed`, `Invalid`, `NotApplicable` — every discrepancy includes explanation and remediation.

### Checks

Store data, products, categories, customers, orders, order items, reviews, media, downloads (N/A), prices, relationships, localization, SEO URLs, manufacturers (N/A), duplicate mappings.

### Script

`scripts/migration/run-smartstore-reconciliation.ps1`

### Documentation

- [`SMARTSTORE-RECONCILIATION.md`](./SMARTSTORE-RECONCILIATION.md)
- [`PHASE-47-PREIMPLEMENTATION.md`](./PHASE-47-PREIMPLEMENTATION.md)

## Tests

`Commerce.Tests.Unit.SmartstoreImport` — 5 reconciliation tests:

| Test | Scenario |
|------|----------|
| `ReconcileAsync_AfterSmallImport_ReportsFullyReconciled` | Clean migration |
| `ReconcileAsync_AfterFullImport_ReportsManufacturerNotApplicable` | Extended entities + N/A |
| `ReconcileAsync_BrokenReferences_ReportsMissingDiscrepancies` | Broken FKs surfaced |
| `ReconcileAsync_WithoutImportRun_Fails` | Guard when no import |
| `ReconcileAsync_AllDiscrepanciesHaveRemediation` | Every gap has remediation |

## Verification

```bash
dotnet test tests/Commerce/Commerce.Tests.Unit.SmartstoreImport/Commerce.Tests.Unit.SmartstoreImport.csproj
```

## Known limitations

1. Reconciliation requires prior import for same SQL file hash
2. Download binary presence not verified (metadata-only media check)
3. Locale string resources classified NotApplicable (framework resources differ)
4. No persisted reconciliation run entity (in-memory report DTO)
