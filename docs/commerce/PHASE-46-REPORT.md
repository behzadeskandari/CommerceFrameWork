# Phase 46 — Smartstore SQL Import & Migration (Report)

**Status:** Complete

## Summary

Phase 46 delivers repeatable Smartstore SQL export migration tooling: schema discovery from the supplied file (no guessed schema), ordered entity importers, legacy ID mapping, transaction-safe orchestration, and full warning/error reporting. Mapping documentation and representative test fixtures are included.

## Data inspection

| Item | Result |
|------|--------|
| `data/smartstore/scriptWithData.sql` | **Not present** in repository |
| Schema discovery | From `CREATE TABLE` / `INSERT` in supplied SQL |
| Test fixtures | Explicit schema in `tests/Commerce/Commerce.Tests.Unit.SmartstoreImport/Fixtures/` |

## Deliverables

### Module (`Commerce.Modules.SmartstoreImport`)

| Layer | Contents |
|-------|----------|
| Domain | `ImportRun`, `ImportIdMapping`, `ImportIssue` |
| Contracts | `ISmartstoreImportService`, `SmartstoreImportOptions`, result DTOs |
| Application | Importer interfaces, table/entity constants |
| Infrastructure | `SmartstoreSqlParser`, `SmartstoreImportService`, 16 entity importers |
| Host | Registered in `Commerce.Host/Program.cs` |

### Import capabilities

- **Repeatable** — file SHA-256 hash; duplicate run blocked by default
- **Idempotent** — `ImportIdMapping` skip on re-import (`AllowDuplicateRun=true`)
- **Transaction-safe** — per-importer transactions on relational DB
- **Issue tracking** — every skipped/partial row logged; never silently discarded
- **Conditional** — importers run only when source tables exist

### Importers implemented

Language, Currency, Store, Setting, Customer, Category, Manufacturer (warn), Product (+ offer + category mappings), Product variant (warn), Media (+ product media), Discount, ProductReview, Order (+ items), Topic, UrlRecord, Localization.

### Not in scope (deferred)

Customer roles/addresses, product attributes/options, downloads binary migration, menus, store mappings, locale string resources (framework differs), admin import UI, host CLI endpoint.

## Documentation

| Document | Purpose |
|----------|---------|
| [`SMARTSTORE-IMPORT-MAPPING.md`](./SMARTSTORE-IMPORT-MAPPING.md) | Entity + field mapping, incompatible fields, issue codes |
| [`PHASE-46-PREIMPLEMENTATION.md`](./PHASE-46-PREIMPLEMENTATION.md) | Design scope |
| [`data/smartstore/README.md`](../../data/smartstore/README.md) | SQL file placement |

## Script

`scripts/migration/run-smartstore-import.ps1` — runs verification tests; `-InspectOnly` for schema test filter.

## Tests

`Commerce.Tests.Unit.SmartstoreImport` — **9 passing**:

| Test | Scenario |
|------|----------|
| `ParseFile_DiscoversTablesAndRows_FromSmallSample` | Parser |
| `InspectSchema_ReportsDiscoveredTables` | Schema report |
| `ImportAsync_SmallMigration_ImportsCoreEntities` | Small migration |
| `ImportAsync_FullMigration_ImportsExtendedEntities` | Full migration |
| `ImportAsync_DuplicateMigration_IsBlockedByDefault` | Duplicate guard |
| `ImportAsync_DuplicateMigration_AllowedWhenConfigured` | Idempotent re-run |
| `ImportAsync_BrokenReferences_ReportsWarningsWithoutSilentDiscard` | Broken FKs |
| `ImportAsync_MissingMedia_ReportsWarnings` | Missing media paths |
| `ImportAsync_InvalidValues_ReportsWarnings` | Invalid rate/rating |

Fixtures: `small-sample.sql`, `full-sample.sql`, `broken-references.sql`, `missing-media.sql`, `invalid-values.sql`.

## Verification

```bash
dotnet test tests/Commerce/Commerce.Tests.Unit.SmartstoreImport/Commerce.Tests.Unit.SmartstoreImport.csproj
./scripts/migration/run-smartstore-import.ps1 -SqlFile tests/Commerce/Commerce.Tests.Unit.SmartstoreImport/Fixtures/small-sample.sql
```

## Known limitations

1. Real `scriptWithData.sql` not validated until file is added to `data/smartstore/`
2. Media metadata only — binaries must be copied separately
3. Manufacturer / variant attribute / locale resources: warnings, not full entity import
4. Production import currently via `ISmartstoreImportService` in-process; dedicated admin API deferred
5. Host-wide build may have unrelated blockers; SmartstoreImport module and tests build independently

## Next steps (outside Phase 46)

- Add `scriptWithData.sql` and run full production import on staging
- Admin API endpoint for operator-triggered import
- Attribute/option importers for full variant fidelity
- Customer password migration strategy (if required)
