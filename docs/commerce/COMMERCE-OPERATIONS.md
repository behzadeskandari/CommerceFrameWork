# Commerce — Operations Guide

**Purpose:** Run, monitor, and troubleshoot the Commerce platform in production.

---

## Health checks

### Endpoints

| Endpoint | Use | Expected |
|---|---|---|
| `GET /health/live` | Kubernetes liveness probe | `Healthy` while process runs |
| `GET /health/ready` | Load balancer / K8s readiness | `Healthy` when dependencies OK |
| `GET /health` | Full diagnostic JSON | Review all `entries` |

All endpoints return JSON:

```json
{
  "status": "Healthy",
  "totalDurationMs": 12.4,
  "entries": {
    "database": { "status": "Healthy", "description": "Database connection succeeded." },
    "scheduling": { "status": "Healthy", "description": "Background job processor is healthy." }
  }
}
```

### Dependency checks

| Check | Healthy | Degraded | Unhealthy |
|---|---|---|---|
| **database** | DB connects | — | Connection failed |
| **cache** | Probe OK or not configured | — | Probe failed |
| **scheduling** | Processor cycled, no dead letters | Dead-letter jobs exist | Processor never ran |
| **plugins** | No required plugin failures | Optional plugin failed | Required plugin failed |
| **modules** | All required modules OK | — | Required module failed |
| **payment_providers** | Providers registered & configured | None registered / misconfigured | — |

### Kubernetes example

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 15

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 20
  periodSeconds: 10
```

---

## Correlation and logging

### Headers

| Header | Direction | Description |
|---|---|---|
| `X-Correlation-ID` | Request + Response | End-to-end business correlation (client may supply) |
| `X-Request-ID` | Request + Response | Unique ID for this HTTP request |

Pass `X-Correlation-ID` from storefront/API clients to trace a purchase flow across services.

### Log scope fields

Structured scopes include:

| Field | When present |
|---|---|
| `CorrelationId` | HTTP requests, commerce operations, background jobs |
| `RequestId` | HTTP requests |
| `Operation` | e.g. `cart.item.added`, `checkout.started`, `payment.created` |
| `CartId`, `CheckoutId`, `OrderId`, `PaymentId` | Entity-specific operations |

### Correlating a purchase flow

Filter logs by a single `CorrelationId` to see:

```
http.request.start → cart.item.added → checkout.started → payment.created → order.created → notification.dispatch
```

Background notification retries preserve correlation via job payload `__correlationId`.

### Secret masking

The platform **never logs**:

- Passwords, API keys, webhook secrets
- Bearer tokens (redacted to `Bearer ***`)
- Connection string secrets

If you add custom log properties, avoid keys containing `password`, `secret`, `token`, `apikey`, `cvv`, or `cardnumber`.

---

## Metrics

Built-in `System.Diagnostics.Metrics` (compatible with OpenTelemetry .NET SDK):

| Metric | Tags | Description |
|---|---|---|
| `commerce.http.requests` | `method`, `status_code` | HTTP throughput |
| `commerce.cart.operations` | `operation` | Cart mutations |
| `commerce.checkout.operations` | `operation` | Checkout lifecycle |
| `commerce.payment.operations` | `operation` | Payment actions |
| `commerce.order.operations` | `operation` | Order lifecycle |
| `commerce.notification.operations` | `operation` | Notification dispatch |

Meter: **`Commerce`** version `1.0.0`

To export to Prometheus/Grafana, add OpenTelemetry hosting packages and configure an OTLP or Prometheus exporter in the host — not included in Phase 38 defaults.

---

## Tracing

`ActivitySource` name: **`Commerce`**

HTTP requests create `http.request` activities with tags:

- `correlation.id`
- `request.id`
- `http.method`
- `http.route`

Enable a .NET OTel listener or `dotnet-counters` for local inspection.

---

## Background jobs

- Processor polls every **5 seconds** (default)
- Health check reports `lastCycleUtc`, `pendingJobs`, `deadLetterJobs`
- Jobs enqueued during HTTP requests inherit the request's `CorrelationId`

If `scheduling` is **Degraded**:

1. Check `/api/admin/scheduling/jobs` for dead-letter entries
2. Review logs with `Operation=background.job.execute`
3. Retry or cancel stuck jobs via admin API

---

## Audit + observability

Audit entries store the same `CorrelationId` as application logs. Cross-reference:

1. Find `CorrelationId` in HTTP response header
2. Query audit: `GET /api/admin/audit?Action=...` (filter in application)
3. Search logs for the same `CorrelationId`

---

## Disaster recovery and backups

See **[DISASTER-RECOVERY.md](./DISASTER-RECOVERY.md)** for the full runbook.

- **RPO:** 24 hours (daily backups by default)
- **RTO:** 4 hours (documented full-restore procedure)
- Create backup: `POST /api/admin/disaster-recovery/backups/create`
- **Do not treat backups as production-valid until recovery testing passes** (`POST .../backups/{id}/recovery-test`)
- `/health/ready` includes `backups` — **Degraded** if latest backup lacks recovery test; **Unhealthy** if backup is stale

---

## Production checklist

- [ ] Configure log aggregation (Application Insights, ELK, CloudWatch, etc.)
- [ ] Set up alerts on `/health/ready` != Healthy
- [ ] Alert on `scheduling` dead-letter count > 0
- [ ] Alert on required `plugins` or `modules` Unhealthy
- [ ] Pass `X-Correlation-ID` from storefront BFF
- [ ] Do **not** enable debug logging for payment provider HTTP in production
- [ ] Review Phase 37 audit retention policy (`Audit:Retention:RetentionDays`)
- [ ] Schedule daily backups and monthly recovery tests (see `DISASTER-RECOVERY.md`)
- [ ] Alert on `/health/ready` `backups` != Healthy

---

## Related documentation

- [PHASE-38-REPORT.md](./PHASE-38-REPORT.md) — implementation details
- [PHASE-37-REPORT.md](./PHASE-37-REPORT.md) — audit and security
- [PHASE-43-REPORT.md](./PHASE-43-REPORT.md) — disaster recovery
- [PHASE-44-REPORT.md](./PHASE-44-REPORT.md) — Docker deployment
- [PHASE-45-REPORT.md](./PHASE-45-REPORT.md) — automated verification
- [TEST-VERIFICATION.md](./TEST-VERIFICATION.md) — test suite guide
- [DISASTER-RECOVERY.md](./DISASTER-RECOVERY.md) — backup and restore runbook
- [DEPLOYMENT.md](./DEPLOYMENT.md) — Docker Compose, HTTPS, rollback
- [ENVIRONMENT-CONFIGURATION.md](./ENVIRONMENT-CONFIGURATION.md) — secrets and env matrix
