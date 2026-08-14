# Phase 47 — Smartstore Migration Validation & Reconciliation (Pre-Implementation)

## Goal

Compare original Smartstore SQL export data against Commerce after import, generate reconciliation reports, and classify every discrepancy with explanation and remediation.

## Scope

Automated checks for:

- Store data (language, currency, store, settings)
- Product, category, customer counts
- Order and order item counts
- Reviews, media, prices
- Downloads (not-applicable until importer exists)
- Relationships (product-category, product-media, order refs)
- Localization and SEO URLs
- Duplicate legacy ID mappings

## Classification model

| Classification | Meaning |
|----------------|---------|
| `Match` | Source record mapped and verified in Commerce |
| `Missing` | Source record not imported or relationship unresolved |
| `Duplicate` | Multiple mappings for same legacy Id |
| `Transformed` | Imported with intentional field transformation |
| `Invalid` | Import error or stale/broken mapping |
| `NotApplicable` | No Commerce target (e.g. Manufacturer, Download, LocaleStringResource) |

## API

`ISmartstoreReconciliationService.ReconcileAsync(SmartstoreReconciliationOptions)`

Requires a prior successful import run for the same SQL file hash.

## Tests

`Commerce.Tests.Unit.SmartstoreImport` — reconciliation workflow tests

## Documentation

- `docs/commerce/SMARTSTORE-RECONCILIATION.md`
