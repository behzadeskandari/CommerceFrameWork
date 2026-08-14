# Phase 43 — Disaster Recovery / Backup / Restore (Report)

**Status:** Complete

## Summary

Phase 43 adds a Disaster Recovery module with coordinated backups, retention, checksum verification, recovery testing, integrity reporting, scheduled jobs, health checks, and operator documentation.

## Deliverables

### Module (`Commerce.Modules.DisasterRecovery`)

| Layer | Contents |
|-------|----------|
| Contracts | `IBackupService`, verification/recovery/integrity APIs, validity enums |
| Domain | `BackupRun`, `BackupArtifact`, `RecoveryTestRun` |
| Application | Backup orchestration, retention, verification, recovery test, metadata (RPO/RTO) |
| Infrastructure | SQL backup/verify, file archivers, integrity probe, jobs, health probe, admin permissions |

### Backup components

1. **Database** — `BACKUP DATABASE` to `.bak` (SQL Server; skipped for in-memory)
2. **Media** — zip of `App_Data/media/`
3. **Downloads** — integrity manifest (files in media backup)
4. **Configuration** — appsettings + masked DB install file + settings export
5. **Plugins** — zip of `Plugins/`
6. **Manifest** — checksums + integrity snapshot

### Validity model

| Status | Meaning |
|--------|---------|
| `Unverified` | Backup created |
| `ChecksumVerified` | SHA-256 checks passed |
| `RestoreTested` | Recovery test passed — **only this is valid for recovery** |

### RPO / RTO

- **RPO:** 24 hours (configurable via backup schedule)
- **RTO:** 4 hours (documented restore procedure)

### Admin API

`api/admin/disaster-recovery/*` — create, list, verify, recovery-test, retention, integrity, targets

### Operations

- Scheduled jobs: `backup.create`, `backup.retention` (daily)
- Health check: `backups` on `/health/ready`
- Runbook: [`DISASTER-RECOVERY.md`](./DISASTER-RECOVERY.md)
- Script: `scripts/disaster-recovery/verify-backup.ps1`

### Tests

`Commerce.Tests.Unit.DisasterRecovery` — validity rules, secret masking, RPO/RTO metadata

## Verification

```bash
dotnet build src/Commerce/Modules/DisasterRecovery/Commerce.Modules.DisasterRecovery/Commerce.Modules.DisasterRecovery.csproj
dotnet test tests/Commerce/Commerce.Tests.Unit.DisasterRecovery/Commerce.Tests.Unit.DisasterRecovery.csproj
```

## Security

- Configuration backups mask connection string passwords by default
- Recovery test uses isolated staging folder under `App_Data/backups/_recovery-tests/`
- Permissions: `DisasterRecovery.*`

## Constraints

- Native SQL backup path must be accessible to SQL Server
- In-memory dev databases skip DB backup component
- Checksum verification alone does **not** mark backups as production-valid
