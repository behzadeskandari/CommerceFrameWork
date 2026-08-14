# PHASE 27 — Customer Notifications / Email / SMS — Pre-Implementation

**Status:** Pre-implementation  
**Date:** 2026-08-12

---

## Objective

Introduce provider-independent notification infrastructure for email, SMS, and in-app messages, driven by configurable templates and commerce lifecycle events.

---

## Scope

### In scope

- `Commerce.Notifications.*` module (Domain, Contracts, Application, Infrastructure)
- Channel abstractions: Email (`IEmailSender`), SMS (`ISmsSender`), In-app (persisted entity)
- Template model: system name, event type, channel, subject, body, language, store, variables, enabled
- Event handlers wired via existing cross-module handler interfaces (not a full event bus)
- Delivery log with retry scheduling (hosted poller; Phase 28 will extend background jobs)
- Admin API: template CRUD, delivery history, manual retry
- Storefront API: unread in-app notifications + mark read
- Admin UI: template list/form, notification history
- Unit tests + documentation

### Out of scope

- Vendor-specific SMTP/SMS implementations (use framework logging stubs)
- Full background job platform (Phase 28)
- Distributed event bus (Phase 34)
- Shipment entity integration (handler ready; source module call deferred until shipments exist)

---

## Event mapping

| Event | Handler interface | Source module |
|---|---|---|
| CustomerRegistered | `ICustomerRegisteredHandler` | Customers |
| OrderCreated | `IOrderCreatedHandler` | Orders |
| PaymentSucceeded | `IOrderPaidHandler` | Orders (payment sync) |
| PaymentFailed | `IOrderPaymentFailedHandler` | Orders (payment sync) |
| OrderCancelled | `IOrderCancelledHandler` | Orders |
| ShipmentCreated | `IShipmentCreatedHandler` | (handler only) |
| RefundCreated | `IOrderRefundHandler` | Orders (payment sync) |
| DownloadAvailable | `IDownloadAvailableHandler` | Downloads |

---

## Security

- Admin endpoints protected by `Notifications.View` / `Notifications.Manage`
- Recipients masked in admin history list DTOs
- Full recipient/body retained in logs for retry (admin-only access)
- Provider credentials remain in framework/infrastructure configuration (not in templates)

---

## Dependencies

- Framework: `IEmailSender`, new `ISmsSender`
- Customers, Orders, Downloads contracts for handler interfaces and read models

---

## Deliverables checklist

- [ ] Backend module + host registration
- [ ] Migrations contributor
- [ ] Admin + storefront controllers
- [ ] Admin UI + API client
- [ ] Unit tests
- [ ] PHASE-27-REPORT.md + roadmap update

**STOP after Phase 27.**
