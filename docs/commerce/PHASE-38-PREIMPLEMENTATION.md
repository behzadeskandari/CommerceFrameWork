# PHASE 38 — Observability — Pre-implementation

**Status:** Pre-implementation  
**Date:** 2026-08-13

---

## Scope

Operational observability for Commerce: structured logging, correlation IDs, metrics, tracing, and health endpoints.

### Capabilities

| Capability | Implementation |
|---|---|
| Structured logging | `ILogger` scopes with `CorrelationId`, `RequestId`, `Operation`, entity IDs |
| Correlation IDs | `X-Correlation-ID` middleware + `ICorrelationContext` |
| Request IDs | `X-Request-ID` generated per HTTP request |
| Metrics | `System.Diagnostics.Metrics` counters (`commerce.*.operations`) |
| Tracing | `ActivitySource` (`Commerce`) with correlation tags |
| Health | `/health/live`, `/health/ready`, `/health` |

### Dependency health checks

- Database (`CommerceDbContext.CanConnectAsync`)
- Cache (memory/distributed probe, or "not configured")
- Background jobs (`ISchedulingHealthProbe`)
- Plugins (`ICommercePluginManager`)
- Modules (`ICommerceModuleRegistry`)
- Payment providers (`IPaymentProviderHealthProbe`)

### Correlation flow

```
HTTP Request
  → CorrelationIdMiddleware
  → Cart / Checkout / Payment / Order / Notification (scoped logs + metrics)
  → Background jobs (correlation embedded in job payload)
  → Audit entries (via HttpAuditActorContext)
```

Secrets are masked by `LogSanitizer` — passwords, tokens, API keys are never logged.

---

## Module layout

```
Commerce.Framework.Contracts.Observability  → ICorrelationContext, health probes
Commerce.Framework.Application.Observability → CommerceLogging, CommerceMetrics, CommerceTracing
Commerce.Observability.Application          → LogSanitizer
Commerce.Observability.Infrastructure       → middleware, health checks
Commerce.Modules.Observability              → module registration
```

---

## Out of scope (Phase 38)

- Serilog / OpenTelemetry OTLP exporter configuration
- Grafana / Prometheus scrape endpoints
- Distributed tracing across external payment gateways (metadata only)
