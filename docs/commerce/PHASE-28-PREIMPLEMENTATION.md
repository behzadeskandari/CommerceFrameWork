# PHASE 28 — Background Jobs & Scheduling — Pre-Implementation

**Status:** Pre-implementation  
**Date:** 2026-08-12

---

## Objective

Build provider-independent, durable background job infrastructure with retry, dead-letter, distributed locking, and admin visibility.

---

## Scope

### In scope

- `Commerce.Framework.Scheduling` abstractions (`IBackgroundJobScheduler`, `IBackgroundJobHandler`, `IJobLockProvider`)
- `Commerce.Scheduling.*` module with persisted jobs, executions, recurring schedules, distributed locks
- Job kinds: immediate, delayed, scheduled, recurring
- Retry with max attempts, failure and dead-letter states, cancellation
- `BackgroundJobProcessor` hosted worker (DB-backed, restart-safe)
- Admin API + UI for job history and recurring schedules
- Integrate Phase 27 notification retry and Phase 24 search indexing
- Stub handlers for future email/SMS/reports/cleanup/downloads/inventory/promotions/plugin tasks
- Unit tests + documentation

### Out of scope

- External scheduler vendors (Hangfire, Quartz as hard dependency)
- Multi-node leader election beyond DB locks
- Cron expression parser (interval-based recurring in Phase 28)

---

## Integration points

| Consumer | Job type | Change |
|---|---|---|
| Notifications | `notifications.retry`, `notifications.deliver` | Replace `NotificationRetryHostedService` |
| Search | `search.index.process` | Remove sync index processing from catalog hooks |
| Future modules | stub job types | Handlers registered, no-op logging |

---

## STOP after Phase 28.
