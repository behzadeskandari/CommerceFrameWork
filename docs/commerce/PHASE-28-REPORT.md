# PHASE 28 — Background Jobs & Scheduling — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## Summary

Phase 28 delivers durable, provider-independent background job infrastructure. Jobs are persisted in the database, processed by a hosted worker with atomic claim semantics, and support immediate, delayed, scheduled, and recurring execution with retry, dead-letter, cancellation, and distributed locking.

---

## Backend Delivered

### Framework: `Commerce.Framework.Scheduling`

| Abstraction | Purpose |
|---|---|
| `IBackgroundJobScheduler` | Enqueue, schedule, delay, register recurring, cancel |
| `IBackgroundJobHandler` | Module-specific job execution |
| `IJobLockProvider` | Distributed lock acquisition |
| `BackgroundJobTypes` | Standard job type constants |

No external scheduler vendor is required.

### Module: `Commerce.Scheduling`

| Layer | Contents |
|---|---|
| Domain | `BackgroundJob`, `BackgroundJobExecution`, `RecurringJobSchedule`, `JobDistributedLock` |
| Application | `BackgroundJobScheduler`, `BackgroundJobExecutor`, `BackgroundJobProcessor`, admin service, stub handlers |
| Infrastructure | EF repository with atomic claim SQL, permissions, migration contributor |
| Module | `SchedulingModule` (required), registers default recurring jobs on startup |

### Job capabilities

| Feature | Implementation |
|---|---|
| Immediate jobs | `EnqueueAsync` → Pending, processed on next poll |
| Delayed jobs | `EnqueueDelayedAsync` → Scheduled with future `ExecuteAtUtc` |
| Scheduled jobs | `ScheduleAsync` → one-time future execution |
| Recurring jobs | `RecurringJobSchedule` + interval enqueue with idempotency |
| Retry | Exponential backoff (2^n minutes), `MaxAttempts` (default 3) |
| Dead letter | Status `DeadLetter` when attempts exhausted |
| Cancellation | `CancelAsync` / admin cancel |
| Job history | `BackgroundJobExecution` per attempt |
| Duplicate prevention | Idempotency keys + conditional claim UPDATE |
| Distributed locking | `JobDistributedLock` table for recurring enqueue |
| Concurrency control | Atomic `TryClaimJobAsync` (status-guarded UPDATE) |

### Prepared job types

| Job type | Handler | Status |
|---|---|---|
| `notifications.retry` | `NotificationRetryJobHandler` | Integrated |
| `notifications.deliver` | `NotificationDeliverJobHandler` | Integrated |
| `search.index.process` | `SearchIndexProcessJobHandler` | Integrated |
| `email.send` | Stub | Ready |
| `sms.send` | Stub | Ready |
| `reports.generate` | Stub | Ready |
| `maintenance.cleanup` | Stub | Recurring (daily) |
| `downloads.expired` | Stub | Ready |
| `inventory.tasks` | Stub | Ready |
| `promotions.tasks` | Stub | Ready |
| `plugins.tasks` | Stub | Ready |

### Default recurring schedules (registered on module start)

| Schedule key | Interval | Job type |
|---|---|---|
| `notifications.retry` | 60s | Notification retry sweep |
| `search.index.process` | 30s | Search index queue processor |
| `maintenance.cleanup` | 86400s | Maintenance stub |

### Permissions

| Permission | Purpose |
|---|---|
| `Scheduling.View` | List jobs and recurring schedules |
| `Scheduling.Manage` | Cancel, retry, enable/disable recurring |

### API Endpoints

**Admin — Jobs**

| Method | Route | Permission |
|---|---|---|
| GET | `/api/admin/scheduling/jobs` | Scheduling.View |
| GET | `/api/admin/scheduling/jobs/{id}` | Scheduling.View |
| POST | `/api/admin/scheduling/jobs/{id}/cancel` | Scheduling.Manage |
| POST | `/api/admin/scheduling/jobs/{id}/retry` | Scheduling.Manage |

**Admin — Recurring**

| Method | Route | Permission |
|---|---|---|
| GET | `/api/admin/scheduling/recurring` | Scheduling.View |
| POST | `/api/admin/scheduling/recurring/{scheduleKey}/enable` | Scheduling.Manage |
| POST | `/api/admin/scheduling/recurring/{scheduleKey}/disable` | Scheduling.Manage |

---

## Architecture

```
IBackgroundJobScheduler → BackgroundJob (DB)
BackgroundJobProcessor (hosted, poll every 5s)
  → RecurringJobSchedule due → enqueue with idempotency + distributed lock
  → ListDueJobs → TryClaimJobAsync (atomic UPDATE)
  → BackgroundJobExecutor → IBackgroundJobHandler
  → BackgroundJobExecution history
  → retry / dead-letter / completed
```

---

## Phase 27 / 24 integration

- **Removed** `NotificationRetryHostedService`; notification failures enqueue `notifications.deliver` delayed jobs
- **Added** recurring `notifications.retry` sweep as safety net
- **Search** catalog hooks now queue index jobs only; recurring worker processes batch asynchronously

---

## Reliability

- Jobs survive application restart (persisted state)
- Duplicate execution prevented via claim UPDATE and idempotency keys
- Temporary failures retried with backoff; permanent failures → dead letter
- Distributed lock prevents duplicate recurring enqueue across instances

---

## Frontend Delivered

| Area | Files |
|---|---|
| API | `scheduling.models.ts`, `scheduling-api.service.ts` |
| Admin pages | Background job list, recurring schedule list |
| Navigation | Admin sidebar under Scheduling permissions |
| Localization | English + Persian keys |

---

## Tests

Project: `Commerce.Tests.Unit.Scheduling`

| Test area | Coverage |
|---|---|
| Scheduler | Idempotency, cancellation |
| Executor | Success, retry, dead letter |
| Domain | Claim deduplication, manual retry reset |
| Authorization | Permission constants |

Run: `dotnet test tests/Commerce/Commerce.Tests.Unit.Scheduling`

---

## Known limitations (by design)

- Interval-based recurring (no full cron parser yet)
- Single DB-backed worker loop (scales horizontally via claim semantics, not sharded queues)
- Stub handlers log only until respective modules wire real work

---

**Phase 28 complete. STOP.**
