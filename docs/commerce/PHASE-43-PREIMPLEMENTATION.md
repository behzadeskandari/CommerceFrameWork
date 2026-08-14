# Phase 43 — Disaster Recovery / Backup / Restore (Pre-Implementation)

## Goal

Design and implement production disaster recovery with documented RPO/RTO, coordinated backups, verification, and recovery testing.

## Scope

- SQL Server native backup (when configured)
- Media, downloads integrity, configuration, plugin packages
- Retention policy
- Checksum verification
- Recovery testing (`RESTORE VERIFYONLY` + staged file restore)
- Live data integrity reporting
- Admin API + scheduled jobs + health check

## Validity rule

Backups are **not valid for recovery** until `RestoreTested` — recovery test must pass.

## RPO / RTO defaults

- RPO: 24 hours (daily backup schedule)
- RTO: 4 hours (documented restore runbook)

## Tests

- `Commerce.Tests.Unit.DisasterRecovery`

## Documentation

- `docs/commerce/DISASTER-RECOVERY.md`
