# PHASE 27 — Customer Notifications / Email / SMS — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## Summary

Phase 27 adds provider-independent customer notification infrastructure with email, SMS, and in-app channels. Templates are scoped by store and language; delivery is logged with exponential backoff retry. Commerce modules raise notifications through existing handler interfaces rather than a dedicated event bus.

---

## Backend Delivered

### Module: `Commerce.Notifications`

| Layer | Contents |
|---|---|
| Domain | `NotificationTemplate`, `NotificationLog`, `InAppNotification`; enums for channel, event type, delivery status |
| Contracts | Admin DTOs, `INotificationEventPublisher`, `INotificationChannelProvider`, storefront in-app DTOs |
| Application | Template renderer/selector, `NotificationDispatcher`, channel providers, event handlers, admin services, `NotificationRetryHostedService` |
| Infrastructure | EF configs, `EfNotificationsRepository`, permissions, migration contributor |
| Module | `NotificationsModule` registered in host |

### Framework extensions

| Component | Purpose |
|---|---|
| `ISmsSender` | SMS abstraction (no vendor coupling) |
| `LoggingSmsSender` | Development stub alongside existing `LoggingEmailSender` |

### Channels

| Channel | Provider | Backend integration |
|---|---|---|
| Email | `EmailNotificationChannelProvider` | `IEmailSender` |
| SMS | `SmsNotificationChannelProvider` | `ISmsSender` |
| In-app | `InAppNotificationChannelProvider` | Persists `InAppNotification` |

### Templates

- Identity via unique `SystemName`
- Subject, body with `{{variable}}` substitution
- Optional `LanguageId`, `StoreId` with best-match selection (store + language scoring)
- `VariablesJson` metadata for admin reference
- Enabled/disabled state with soft delete

### Events (handler pattern)

| Event | Handler |
|---|---|
| CustomerRegistered | `CustomerRegisteredNotificationHandler` |
| OrderCreated | `OrderCreatedNotificationHandler` |
| PaymentSucceeded | `OrderPaidNotificationHandler` |
| PaymentFailed | `OrderPaymentFailedNotificationHandler` |
| OrderCancelled | `OrderCancelledNotificationHandler` |
| ShipmentCreated | `ShipmentCreatedNotificationHandler` |
| RefundCreated | `OrderRefundNotificationHandler` |
| DownloadAvailable | `DownloadAvailableNotificationHandler` |

Cross-module interfaces added in Customers, Orders, and Downloads contracts. Source services invoke `IEnumerable<IHandler>` after successful operations.

**Note:** `IShipmentCreatedHandler` is registered but no shipment module calls it yet — ready for future fulfillment work.

### Retry and failure handling

- Each delivery creates a `NotificationLog` (pending → sent / failed)
- Failed deliveries schedule `NextRetryAtUtc` with exponential backoff (2^n minutes, max 3 attempts)
- `NotificationRetryHostedService` polls eligible logs every minute
- Admin manual retry via `/api/admin/notifications/logs/{id}/retry`

Phase 28 will provide richer background job infrastructure; this phase uses a lightweight hosted service by design.

### Permissions

| Permission | Purpose |
|---|---|
| `Notifications.View` | List templates and delivery history |
| `Notifications.Manage` | CRUD templates, activate/deactivate, retry |

### API Endpoints

**Admin — Templates**

| Method | Route | Permission |
|---|---|---|
| GET | `/api/admin/notifications/templates` | Notifications.View |
| GET | `/api/admin/notifications/templates/{id}` | Notifications.View |
| POST | `/api/admin/notifications/templates` | Notifications.Manage |
| PUT | `/api/admin/notifications/templates/{id}` | Notifications.Manage |
| POST | `/api/admin/notifications/templates/{id}/activate` | Notifications.Manage |
| POST | `/api/admin/notifications/templates/{id}/deactivate` | Notifications.Manage |
| DELETE | `/api/admin/notifications/templates/{id}` | Notifications.Manage |

**Admin — History**

| Method | Route | Permission |
|---|---|---|
| GET | `/api/admin/notifications/logs` | Notifications.View |
| POST | `/api/admin/notifications/logs/{id}/retry` | Notifications.Manage |

**Storefront — In-app**

| Method | Route | Auth |
|---|---|---|
| GET | `/api/notifications/in-app` | Customer |
| POST | `/api/notifications/in-app/{id}/read` | Customer |

---

## Architecture

```
Commerce event (register, order, payment, download)
  → I*Handler in source module
  → INotificationEventPublisher
  → NotificationTemplateSelector (store + language)
  → NotificationTemplateRenderer ({{vars}})
  → NotificationDispatcher
  → INotificationChannelProvider (Email | SMS | InApp)
  → NotificationLog + optional InAppNotification
  → NotificationRetryHostedService (failed/pending retry)
```

No SMTP/SMS vendor is hard-coded. Replace `IEmailSender` / `ISmsSender` implementations via DI for production providers.

---

## Security

- Admin controllers require permission attributes
- Recipient masking in history list (`jo***@example.com`, `09***12` for phone)
- Full recipient/body stored only for operational retry (admin APIs)
- Templates do not store provider credentials

---

## Frontend Delivered

| Area | Files |
|---|---|
| API | `notifications.models.ts`, `notifications-api.service.ts` |
| Admin pages | Template list/form, notification history |
| Navigation | Admin sidebar links under Notifications permissions |
| Localization | English + Persian keys |

---

## Tests

Project: `Commerce.Tests.Unit.Notifications`

| Test area | Coverage |
|---|---|
| Template rendering | Variable substitution, unknown tokens |
| Template selection | Store isolation, language preference, per-channel pick |
| Dispatcher | Provider routing, failure + retry body reuse |
| Authorization | Permission constants |

Run: `dotnet test tests/Commerce/Commerce.Tests.Unit.Notifications`

---

## Host integration

- `NotificationsModule` registered in `Program.cs`
- Projects added to `Commerce.sln`
- Controllers in `Commerce.Host/Notifications/NotificationsControllers.cs`

---

## Known limitations (by design)

- Logging email/SMS senders only (swap providers in DI for production)
- ShipmentCreated handler not yet invoked from a shipment module
- Retry uses hosted polling; Phase 28 will add durable job infrastructure
- Full event bus deferred to Phase 34

---

**Phase 27 complete. STOP.**
