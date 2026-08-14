# Smartstore Migration Reconciliation

**Phase 47** — validate Commerce data against the original Smartstore SQL export after import.

## Prerequisites

1. Run Smartstore import (Phase 46) for the same SQL file
2. Use the same `SqlFilePath` (or specify `ImportRunId`)

```powershell
./scripts/migration/run-smartstore-import.ps1 -SqlFile data/smartstore/scriptWithData.sql
./scripts/migration/run-smartstore-reconciliation.ps1 -SqlFile data/smartstore/scriptWithData.sql
```

Programmatic:

```csharp
await reconciliationService.ReconcileAsync(new SmartstoreReconciliationOptions(sqlFilePath));
```

## Report structure

`SmartstoreReconciliationResult` contains:

| Field | Description |
|-------|-------------|
| `IsFullyReconciled` | `true` when no blocking discrepancies (Missing, Duplicate, Invalid) |
| `CheckSummaries` | Per-area counts and overall classification |
| `Discrepancies` | Every non-match with explanation + remediation |
| `ClassificationCounts` | Totals by classification |

## Checks performed

| Check | Source | Commerce verification |
|-------|--------|----------------------|
| StoreData | Language, Currency, Store, Setting | Mapped entities exist in DB |
| Products | `Product` | `ImportIdMapping` + `Product` row |
| Categories | `Category` | Mapping + `Category` row |
| Customers | `Customer` | Mapping + `Customer` (system accounts = N/A) |
| Orders | `Order` | Mapping + `Order` row |
| OrderItems | `OrderItem` | Line items linked to imported orders/products |
| Reviews | `ProductReview` | Mapping + review row |
| Media | `MediaFile` | Mapping + `MediaAsset` row |
| Downloads | `Download` | **NotApplicable** — no importer yet |
| Prices | `Product.Price` | `ProductOffer` mapping and amount match |
| Relationships | Mapping tables + FK columns | ProductCategory, ProductMedia, order refs |
| Localization | `LocalizedProperty`, `LocaleStringResource` | EntityTranslation; resources N/A |
| SeoUrls | `UrlRecord` | Mapping + slug row |
| Manufacturers | `Manufacturer` | **NotApplicable** — no Commerce entity |
| DuplicateMappings | Import issues | Duplicate legacy ID detection |

## Classification rules

Every discrepancy includes:

- **Explanation** — what differed and why
- **Remediation** — concrete next step (re-import, manual fix, future phase)

Discrepancies are **never hidden**. `NotApplicable` and `Transformed` are reported explicitly.

## Blocking vs informational

| Classification | Blocks `IsFullyReconciled` |
|----------------|----------------------------|
| Missing | Yes |
| Duplicate | Yes |
| Invalid | Yes |
| Transformed | No |
| NotApplicable | No |
| Match | No |

## Example outcomes

**Clean small import:** all entity checks `Match`, `IsFullyReconciled = true`.

**Full sample:** Manufacturer rows `NotApplicable` with remediation; media/SEO `Match`.

**Broken references:** order customer 999, order item product 999, URL product 999 → `Missing` with remediation paths.

## Verification

```bash
dotnet test tests/Commerce/Commerce.Tests.Unit.SmartstoreImport --filter "FullyQualifiedName~SmartstoreReconciliation"
```
