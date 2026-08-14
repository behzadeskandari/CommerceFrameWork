# Disaster Recovery and Backup Runbook

This document defines production disaster recovery (DR) for the Commerce Framework host.

## Objectives

| Metric | Default target | Meaning |
|--------|----------------|---------|
| **RPO** (Recovery Point Objective) | **24 hours** | Maximum acceptable data loss. With daily backups, you may lose up to one day of changes unless more frequent backups are configured. |
| **RTO** (Recovery Time Objective) | **4 hours** | Target time to restore commerce to an operational state after a disaster. |

Tune `Commerce:DisasterRecovery` and external SQL Server backup schedules to meet your SLA.

## What gets backed up

| Asset | Location | Backup component |
|-------|----------|------------------|
| SQL Server database | SQL Server | `database/commerce.bak` (native `BACKUP DATABASE`) |
| Media files | `App_Data/media/` | `media.zip` |
| Digital downloads | Media storage + DB entitlements | Included in media zip + `downloads/downloads-integrity.json` |
| Configuration | `appsettings*.json`, `App_Data/commerce.database.json`, DB `Settings` | `configuration.zip` (secrets masked) |
| Plugin packages | `Plugins/` | `plugins.zip` |
| Backup manifest | Each backup folder | `backup-manifest.json` with SHA-256 checksums and integrity counts |

**Not backed up (rebuildable):** Redis/memory cache, background job transient state (jobs are in DB), temporary files.

## Backup validity rule

> **A backup is not valid for production recovery until a recovery test passes.**

Lifecycle:

1. **Created** — backup job finished (`Unverified`)
2. **Checksum verified** — all artifact hashes match manifest (`ChecksumVerified`)
3. **Recovery tested** — `RESTORE VERIFYONLY` on database backup + staged file restore test passed (`RestoreTested`)

Only backups with `IsValidForRecovery = true` (status `RestoreTested`) should be used for DR decisions.

## Configuration

```json
"Commerce": {
  "DisasterRecovery": {
    "BackupRoot": "App_Data/backups",
    "RetentionDays": 30,
    "MinBackupsToKeep": 7,
    "MaxBackupAgeHoursBeforeAlert": 26,
    "EnableScheduledBackups": true,
    "MediaRoot": "App_Data/media",
    "PluginsRoot": "Plugins",
    "SqlServerBackupPath": "D:\\SQLBackups\\commerce.bak",
    "MaskSecretsInConfigurationBackup": true
  }
}
```

`SqlServerBackupPath` must be a path **accessible by the SQL Server service account**. If native backup fails, use external SQL Agent / Azure automated backups and treat this module's file backups as a supplement.

## Admin API

| Method | Path | Permission |
|--------|------|------------|
| GET | `/api/admin/disaster-recovery/targets` | `DisasterRecovery.View` |
| GET | `/api/admin/disaster-recovery/backups` | `DisasterRecovery.View` |
| POST | `/api/admin/disaster-recovery/backups/create` | `DisasterRecovery.CreateBackup` |
| POST | `/api/admin/disaster-recovery/backups/{id}/verify` | `DisasterRecovery.VerifyBackup` |
| POST | `/api/admin/disaster-recovery/backups/{id}/recovery-test` | `DisasterRecovery.RunRecoveryTest` |
| POST | `/api/admin/disaster-recovery/retention/apply` | `DisasterRecovery.ManageRetention` |
| GET | `/api/admin/disaster-recovery/integrity` | `DisasterRecovery.View` |

## Scheduled jobs

| Job | Interval | Action |
|-----|----------|--------|
| `backup.create` | Daily (86400s) | Full backup |
| `backup.retention` | Daily | Delete backups older than retention (keeping minimum count) |

## Health check

`/health/ready` includes a `backups` check:

- **Unhealthy** — latest backup older than `MaxBackupAgeHoursBeforeAlert`
- **Degraded** — backup exists but has not passed recovery testing
- **Healthy** — latest backup passed recovery testing

## Recovery procedure (full restore)

### Prerequisites

- Validated backup with `RestoreTested` status
- Maintenance window (target RTO: 4 hours)
- SQL Server instance with sufficient disk space
- Commerce host stopped

### Steps

1. **Stop traffic** — take Commerce host offline (load balancer / IIS / container scale-to-zero).
2. **Restore database**
   ```sql
   RESTORE DATABASE [Commerce]
   FROM DISK = N'D:\restores\commerce.bak'
   WITH REPLACE, RECOVERY, CHECKSUM;
   ```
3. **Restore media** — extract `media.zip` to `App_Data/media/`.
4. **Restore plugins** — extract `plugins.zip` to `Plugins/`.
5. **Restore configuration** — extract `configuration.zip`; merge `appsettings.Production.json` and `App_Data/commerce.database.json` (verify connection string).
6. **Verify integrity** — call `GET /api/admin/disaster-recovery/integrity` after startup; resolve media/plugin count mismatches.
7. **Run migrations** — start host; allow module migrations to apply if backup predates a deployment.
8. **Smoke test** — login, product browse, checkout (test mode), plugin health.
9. **Resume traffic** — re-enable load balancer.

### Partial restore

| Scenario | Restore |
|----------|---------|
| Media loss only | `media.zip` + verify integrity |
| Plugin loss only | `plugins.zip` + reinstall from admin if needed |
| Config drift | `configuration.zip` settings export |
| DB corruption | Database `.bak` only (full procedure) |

## Backup verification procedure (monthly)

1. Create backup: `POST /api/admin/disaster-recovery/backups/create`
2. Verify checksums: `POST .../backups/{id}/verify`
3. Run recovery test: `POST .../backups/{id}/recovery-test`
4. Confirm `IsValidForRecovery: true` in response
5. Log result in change management / ops ticket

**Do not mark backups as production-ready without step 3.**

## External SQL Server backups

For production, combine:

- **Commerce module backups** — coordinated file + optional native `.bak`
- **SQL Server Agent / Azure SQL automated backups** — point-in-time recovery
- **Off-site replication** — copy `App_Data/backups/` and SQL backups to secondary storage

## Scripts

See `scripts/disaster-recovery/` for operator helper scripts.

## Related docs

- [COMMERCE-OPERATIONS.md](./COMMERCE-OPERATIONS.md)
- [MIGRATION-PLAN.md](./MIGRATION-PLAN.md) — backup before upgrades
