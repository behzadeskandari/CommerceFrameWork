# Phase 46 — Smartstore SQL Import & Migration (Pre-Implementation)

## Goal

Build repeatable, transaction-safe migration tooling from Smartstore SQL exports into the Commerce platform with explicit entity mapping, legacy ID tracking, and full issue reporting.

## Constraints

- **Do not guess Smartstore schema** — discover tables/columns from the supplied SQL (`CREATE TABLE`, `INSERT`)
- Import only entities whose source tables exist in the export
- Never silently discard records — warnings/errors persisted per row
- Idempotent re-runs via legacy ID mapping (`ImportIdMapping`)
- Stop after migration tooling and documentation (no storefront/admin UI for import in this phase)

## Data source

Expected path: `data/smartstore/scriptWithData.sql` (not currently in repo). Test fixtures under `tests/Commerce/Commerce.Tests.Unit.SmartstoreImport/Fixtures/` provide representative subsets with explicit schema.

## Module layout

| Project | Role |
|---------|------|
| `Commerce.SmartstoreImport.Domain` | `ImportRun`, `ImportIdMapping`, `ImportIssue` |
| `Commerce.SmartstoreImport.Contracts` | `ISmartstoreImportService`, DTOs, options |
| `Commerce.SmartstoreImport.Application` | Importer abstractions, table/entity constants |
| `Commerce.SmartstoreImport.Infrastructure` | SQL parser, orchestration, entity importers |
| `Commerce.Modules.SmartstoreImport` | Module registration |

## Import pipeline

1. Parse SQL → in-memory `SmartstoreParsedDataSet`
2. Optional duplicate-run guard (SHA-256 file hash)
3. Create `ImportRun` audit record
4. Load existing `ImportIdMapping` for idempotent skip
5. Run importers in dependency order (Language → Currency → Store → …)
6. Per-importer transaction on relational databases
7. Persist mappings and issues after each importer
8. Complete run with summary + warning/error counts

## Importer scope (when table present)

| Order | Importer | Source table(s) |
|------:|----------|-------------------|
| 10 | Language | `Language` |
| 20 | Currency | `Currency` |
| 30 | Store | `Store` |
| 40 | Setting | `Setting` |
| 50 | Customer | `Customer` |
| 60 | Category | `Category` |
| 65 | Manufacturer | `Manufacturer` (warn-only — no Commerce entity) |
| 70 | Product | `Product`, `Product_Category_Mapping` |
| 75 | Product variant | `ProductVariantAttributeCombination` (partial/warn) |
| 80 | Media | `MediaFile`, `Product_MediaFile_Mapping` |
| 85 | Discount | `Discount` |
| 90 | Product review | `ProductReview` |
| 100 | Order | `Order`, `OrderItem` |
| 105 | Topic | `Topic` |
| 110 | Url record | `UrlRecord` |
| 120 | Localization | `LocaleStringResource`, `LocalizedProperty` |

## Tests

`Commerce.Tests.Unit.SmartstoreImport`:

- Small migration
- Full migration (extended entities)
- Duplicate migration (blocked by default; allowed with skip)
- Broken references
- Missing media
- Invalid values
- Schema inspection

## Documentation

- `docs/commerce/SMARTSTORE-IMPORT-MAPPING.md` — entity/field mapping
- `docs/commerce/PHASE-46-REPORT.md` — completion report
- `data/smartstore/README.md` — data placement and usage

## Script

`scripts/migration/run-smartstore-import.ps1` — runs unit test suite for verification; inspect mode available.
