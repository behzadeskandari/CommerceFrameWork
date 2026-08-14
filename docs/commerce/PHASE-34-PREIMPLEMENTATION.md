# PHASE 34 — Domain Events / Webhooks / External API — Pre-Implementation

**Status:** Pre-implementation  
**Date:** 2026-08-12

---

## Scope

Phase 34 introduces an integration platform for external systems:

| Area | Approach |
|---|---|
| **Domain events** | EF interceptor dispatches domain events after `SaveChanges` |
| **Integration events** | Public DTO records — never expose domain entities |
| **Event bus** | In-process `IEventBus` with handler registration |
| **Webhooks** | Subscriptions, HMAC signatures, delivery ledger, retry/dead-letter |
| **External API** | API key auth (`ck_{prefix}_{secret}`), scoped access, public DTOs |
| **Idempotency** | Delivery idempotency keys + `ProcessedIntegrationEvent` ledger |

## Integration Events

`OrderCreated`, `OrderPaid`, `OrderCancelled`, `PaymentSucceeded`, `PaymentFailed`, `ProductCreated`, `ProductUpdated`, `CustomerRegistered`, `InventoryChanged`, `ShipmentCreated`, `RefundCreated`

## Bridge Pattern

Existing module handler contracts (`IOrderCreatedHandler`, etc.) publish integration events via bridge handlers — notification and order flows are not rebuilt.

Catalog/inventory domain events map through `IDomainEventIntegrationMapper`.

## Webhook Delivery

- HMAC-SHA256 signature header: `t={unix},v1={hex}`
- 5 attempts with backoff (1m, 5m, 15m, 1h, 4h)
- Dead-letter after max attempts
- Recurring job: `integration.webhooks.deliver` (30s)

## External API

- Header: `X-Api-Key` or `Authorization: Bearer ck_...`
- Scopes: `orders.read`, `products.read`, `customers.read`
- Routes under `/api/external/*`

## Out of Scope

- Kafka/RabbitMQ transport (in-process only)
- OAuth2 for external partners
- Admin UI for webhooks/API clients (API-only this phase)

---

**Next:** Implementation per this plan.
