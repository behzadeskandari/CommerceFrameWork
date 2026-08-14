# PHASE 38 — Observability — Report

**Status:** Complete  
**Date:** 2026-08-13

---

## Summary

Phase 38 adds structured logging with correlation IDs, request IDs, metrics, tracing, and comprehensive health checks. Logs across Cart → Checkout → Payment → Order → Notification share `CorrelationId` and `RequestId` scopes. Secrets are masked before logging.

---

## Backend

### Module

| Project | Role |
|---|---|
| `Commerce.Framework.Contracts.Observability` | `ICorrelationContext`, health probe interfaces |
| `Commerce.Framework.Application.Observability` | `CommerceLogging`, `CommerceMetrics`, `CommerceTracing` |
| `Commerce.Observability.Application` | `LogSanitizer` |
| `Commerce.Observability.Infrastructure` | Correlation/request middleware, health checks |
| `Commerce.Modules.Observability` | Module registration |

### Middleware pipeline

```
InstallationGate
→ UseCommerceCorrelation()      // X-Correlation-ID, X-Request-ID, Activity tags
→ UseCommerceRequestLogging()   // structured request start/complete logs
→ SecurityHeaders / Store / Auth / ...
```

### Health endpoints

| Endpoint | Purpose |
|---|---|
| `GET /health/live` | Liveness — process alive |
| `GET /health/ready` | Readiness — DB, cache, scheduling, plugins, modules, payment providers |
| `GET /health` | Full health report (JSON) |

### Correlation integration

| Service | Operation scope | Metric |
|---|---|---|
| `CartService` | `cart.item.added` | `commerce.cart.operations` |
| `CheckoutService` | `checkout.started` | `commerce.checkout.operations` |
| `PaymentService` | `payment.created` | `commerce.payment.operations` |
| `OrderService` | `order.created` | `commerce.order.operations` |
| `NotificationDispatcher` | `notification.dispatch` | `commerce.notification.operations` |
| `BackgroundJobScheduler` | embeds correlation in job payload | — |
| `BackgroundJobExecutor` | `background.job.execute` scope | — |
| `HttpAuditActorContext` | uses `ICorrelationContext` | — |

### Metrics (OpenTelemetry-compatible)

- `commerce.http.requests` — method, status_code
- `commerce.cart.operations`
- `commerce.checkout.operations`
- `commerce.payment.operations`
- `commerce.order.operations`
- `commerce.notification.operations`

Meter name: `Commerce` v1.0.0

---

## Tests

`Commerce.Tests.Unit.Observability` — **3 tests**:

1. LogSanitizer masks sensitive keys and Bearer tokens
2. Job payload round-trips correlation ID
3. Commerce metrics/tracing sources registered

---

## Operational documentation

See [COMMERCE-OPERATIONS.md](./COMMERCE-OPERATIONS.md) for runbooks, health check interpretation, log correlation queries, and production checklist.

---

## Verification

```bash
dotnet build src/Commerce/Modules/Observability/Commerce.Observability.Infrastructure/Commerce.Observability.Infrastructure.csproj
dotnet test tests/Commerce/Commerce.Tests.Unit.Observability/Commerce.Tests.Unit.Observability.csproj
curl https://localhost:5100/health/live
curl https://localhost:5100/health/ready
```
