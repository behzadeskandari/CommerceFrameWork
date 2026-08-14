# Smartstore reference data

Place the Smartstore 6.4 SQL export at:

```
data/smartstore/scriptWithData.sql
```

## Status

**`scriptWithData.sql` is not currently present in this repository.**

Phase 46 import tooling inspects the supplied SQL at runtime:

1. Parses `CREATE TABLE` to discover schema (no guessing)
2. Parses `INSERT` statements into in-memory datasets
3. Runs entity importers only for tables that exist
4. Writes an import report with warnings/errors for every skipped or partial record

## Usage

```powershell
./scripts/migration/run-smartstore-import.ps1 -SqlFile data/smartstore/scriptWithData.sql
```

Or inspect schema only:

```powershell
./scripts/migration/run-smartstore-import.ps1 -SqlFile data/smartstore/scriptWithData.sql -InspectOnly
```

After import, run reconciliation (Phase 47):

```powershell
./scripts/migration/run-smartstore-reconciliation.ps1 -SqlFile data/smartstore/scriptWithData.sql
```

See `docs/commerce/SMARTSTORE-RECONCILIATION.md` for discrepancy classifications.

## Test fixtures

Representative subsets for automated tests live under:

`tests/Commerce/Commerce.Tests.Unit.SmartstoreImport/Fixtures/`

These fixtures include explicit `CREATE TABLE` sections so column names are discovered from the file itself.
