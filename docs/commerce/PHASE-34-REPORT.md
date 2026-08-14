# PHASE 34 — Domain Events / Webhooks / External API — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## Summary

Phase 34 delivers domain event dispatch, integration events, webhook subscriptions with signed delivery and retry, API key authentication for external consumers, and idempotency at delivery and consumer levels. Internal domain entities are never exposed — only integration event records and external API DTOs.

---

## Framework

| Component | Path |
|---|---|
| `Commerce.Framework.Events` | `IEventBus`, `InProcessEventBus`, `DomainEventDispatcher`, `DomainEventSaveChangesInterceptor` |
| Wiring | `AddCommerceEvents()` in Integration module; interceptor registered in `CommerceDbContextRegistration` |

---

## Integration Module

**Path:** `src/Commerce/Modules/Integration/`

| Layer | Contents |
|---|---|
| Domain | `WebhookSubscription`, `WebhookDelivery`, `ApiClient`, `ProcessedIntegrationEvent` |
| Contracts | Integration events, webhook/API admin contracts, external order DTOs |
| Application | Signature service, webhook dispatch/delivery, API client auth, event bridges, external order service |
| Infrastructure | EF repository, EF configurations, permissions, migration contributor |
| Module | `IntegrationModule` — registers events, recurring webhook job |

---

## Integration Events (11)

Published via bridge handlers from Orders/Customers notifications and via domain event mapper for Catalog/Inventory:

`OrderCreated`, `OrderPaid`, `OrderCancelled`, `PaymentSucceeded`, `PaymentFailed`, `ProductCreated`, `ProductUpdated`, `CustomerRegistered`, `InventoryChanged`, `ShipmentCreated`, `RefundCreated`

---

## Webhooks

- Admin CRUD + secret rotation + delivery history
- Per-subscription idempotency key: `{subscriptionId}:{eventId}`
- HMAC-SHA256 payload signing
- Retry with exponential-style delays; dead-letter after 5 attempts
- Background job: `BackgroundJobTypes.WebhookDeliveryProcess`

---

## External API

- API keys: `ck_{prefix}_{secret}`, SHA-256 hash stored
- Middleware: `ApiKeyAuthenticationMiddleware` on `/api/external/*`
- Scope filter: `[RequireApiScope]`
- Sample endpoint: `GET /api/external/orders` (maps to `ExternalOrderSummaryDto` / `ExternalOrderDetailDto`)

---

## Admin API

| Route | Purpose |
|---|---|
| `/api/admin/webhooks` | Webhook subscription CRUD, deliveries, rotate secret |
| `/api/admin/api-clients` | API client CRUD, revoke, one-time key on create |

Permissions: `Integration.Webhooks.View|Manage`, `Integration.ApiClients.View|Manage`

---

## Tests

**Project:** `Commerce.Tests.Unit.Integration` — 7 passing tests

| Test | Coverage |
|---|---|
| Signature compute/verify | HMAC validation |
| Invalid signature | Rejection |
| Delivery retry | Dead-letter after max attempts |
| API key auth | Valid/invalid keys |
| Event idempotency | Duplicate consumer mark rejected |
| Webhook dispatch | Duplicate event → single delivery |

---

## Build Notes

- `Commerce.Modules.Integration` builds successfully
- Full solution has pre-existing unrelated build errors (Themes, Plugins, etc.)
- Phase 34 tests run in isolated test project to avoid those blockers

---

**Phase 34 complete. STOP.**
