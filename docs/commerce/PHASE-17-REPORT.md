# PHASE 17 — Commerce Payment Engine — Report

**Status:** Complete (backend compile/test blocked by .NET 10 SDK in this environment)  
**Date:** 2026-08-12

---

## 1. Summary

Phase 17 delivers a production-oriented, provider-independent payment engine. The core `Commerce.Payments` module owns payment lifecycle, transactions, refunds, and callbacks. Provider-specific logic lives in `Commerce.Plugin.Payment.Manual` as the first plugin. Checkout discovers methods via `IPaymentMethodProvider`; processing uses `IPaymentProvider`. Orders sync payment status through `IOrderPaymentSyncService`. No banking dependencies; no card data storage.

---

## 2. Architecture

### Engine vs provider separation

```text
Commerce.Payments (core)
  → IPaymentService, IPaymentProvider contract
  → Payment, PaymentTransaction, Refund entities
  → PaymentService orchestration

Commerce.Plugin.Payment.Manual (provider)
  → ManualPaymentProvider : IPaymentProvider
  → bank-transfer, cash-on-delivery, free methods
```

### Payment flow

```text
Checkout (ReadyForOrder, method selected)
  → POST /api/orders (idempotent)
  → POST /api/payments (idempotent) — or auto for free orders
  → IPaymentProvider.CreatePaymentAsync
  → Payment + PaymentTransaction + PaymentAttempt
  → Callback/verify (server-side only)
  → Order payment status sync
```

### Checkout boundary

- **Discovery:** `PaymentCheckoutMethodProvider` implements existing `IPaymentMethodProvider`
- **Processing:** separate storefront/admin payment APIs — never in checkout controllers
- **Free orders:** `GrandTotal == 0` skips paid methods; auto-capture via free method on order create

---

## 3. Projects Created

| Project | Purpose |
|---|---|
| `Commerce.Payments.Domain` | Payment aggregates, enums, transaction history |
| `Commerce.Payments.Contracts` | `IPaymentProvider`, `IPaymentService`, admin/callback contracts |
| `Commerce.Payments.Application` | Orchestration, checkout bridge, order sync, admin services |
| `Commerce.Payments.Infrastructure` | EF, permissions, settings, seeder, migrations |
| `Commerce.Modules.Payments` | Module registration |
| `Commerce.Plugin.Payment.Manual` | First payment provider (manual/COD/free) |

**Paths:**
- `src/Commerce/Modules/Payments/`
- `src/Commerce/Plugins/Payment/Commerce.Plugin.Payment.Manual/`

---

## 4. Domain Model

| Entity | Purpose |
|---|---|
| `Payment` | Order payment aggregate — amount, status, provider refs, idempotency key |
| `PaymentTransaction` | Every provider interaction (Sale, Capture, Void, Refund, Verification) |
| `PaymentMethod` | Store-scoped method configuration |
| `PaymentAttempt` | Initiation/retry tracking |
| `Refund` / `RefundTransaction` | Refund lifecycle |
| `PaymentCallbackRecord` | Idempotent callback audit |

### Payment status lifecycle

`Pending` → `Initiated` / `RedirectRequired` / `Authorized` → `Captured`  
Failure paths: `Failed`, `Cancelled`  
Refund paths: `PartiallyRefunded`, `Refunded`

### Order payment status (Orders module)

`Pending` → `Authorized` → `Paid` → `PartiallyRefunded` / `Refunded` / `Failed`

Order domain methods added: `ApplyPaymentAuthorized`, `MarkPaymentPaid`, `MarkPaymentFailed`, `ApplyPartialRefund`, `ApplyFullRefund` with payment history entries.

---

## 5. Provider Contract

`IPaymentProvider` (in `Payments.Contracts`, no ASP.NET dependency):

- `CreatePaymentAsync`
- `GetPaymentStatusAsync`
- `VerifyPaymentAsync`
- `CaptureAsync`
- `VoidAsync`
- `RefundAsync`

Manual provider demonstrates all operations for bank transfer, COD, and free orders.

---

## 6. Idempotency & Security

- `Idempotency-Key` header on payment creation (unique per store)
- Unique payment per order constraint
- Callback dedup via `PaymentCallbackRecord` (provider + callback key)
- Server-side amount/currency from authoritative order totals
- Never stores CVV, full card numbers, or provider secrets in payment entities
- Store isolation on all payment queries
- Permissions on all admin mutations

---

## 7. Database Schema

| Table | Notes |
|---|---|
| `Payments` | OrderId, StoreId, Amount, Status, ProviderSystemName, IdempotencyKey |
| `PaymentTransactions` | Full transaction audit trail |
| `PaymentMethods` | Store-scoped configuration |
| `PaymentAttempts` | Retry tracking |
| `Refunds` / `RefundTransactions` | Refund history |
| `PaymentCallbackRecords` | Callback idempotency |

Migration: `PaymentsInitialMigration` via `ICommerceMigration`.

---

## 8. Module Registration

```csharp
modules.AddModule<PaymentsModule>();  // after Tax, before Orders
builder.Services.AddManualPaymentProvider();
```

Seeder: `Commerce:Payments:SeedDevelopmentData=true`  
Seeds: Bank Transfer, Cash on Delivery, Free methods.

---

## 9. API Endpoints

### Storefront
| Endpoint | Description |
|---|---|
| `POST /api/payments` | Create payment for order (Idempotency-Key) |
| `GET /api/payments/{id}` | Payment detail |
| `GET /api/payments/by-order/{orderId}` | Payment by order |
| `POST /api/payments/callback/{provider}` | Provider callback pipeline |

### Admin
| Endpoint | Permission |
|---|---|
| `GET /api/admin/payments` | `Payments.View` |
| `GET /api/admin/payments/{id}` | `Payments.View` |
| `GET /api/admin/payments/{id}/transactions` | `Payments.View` |
| `POST /api/admin/payments/{id}/capture` | `Payments.Manage` |
| `POST /api/admin/payments/{id}/void` | `Payments.Manage` |
| `POST /api/admin/payments/{id}/refund` | `Payments.Refund` |
| `POST /api/admin/payments/{id}/partial-refund` | `Payments.Refund` |
| CRUD `/api/admin/payment-methods` | `Payments.Configure` |

---

## 10. Permissions

| Permission | Description |
|---|---|
| `Payments.View` | View payments and transactions |
| `Payments.Manage` | Capture, void |
| `Payments.Refund` | Full and partial refunds |
| `Payments.Configure` | Payment method CRUD |

---

## 11. Angular Changes

### Admin
- `/payments` — list with status filter
- `/payments/:id` — detail, transactions, capture/void/refund actions
- `/payments/methods` — payment method configuration CRUD

### Storefront
- Checkout payment step (existing, now populated from backend)
- Post-order payment creation in `finalize()`
- `/payment/processing`, `/payment/success`, `/payment/failed`
- EN + FA localization

---

## 12. Tests

| Suite | Coverage |
|---|---|
| `PaymentStateTests` | Payment state transitions, refunds, order payment sync |
| `PaymentsArchitectureTests` | Layer boundaries, no provider in core |
| `PaymentFlowTests` | Admin method CRUD, seeded methods in checkout |

**Frontend:** `npm test` — **PASS** (4/4)  
**Frontend build:** `npm run build` — **PASS**

**Backend:** **BLOCKED** — SDK 8.0.302; projects target `net10.0`

---

## 13. Build Results

| Command | Result |
|---|---|
| `dotnet build Commerce.sln -c Release` | **BLOCKED** (NETSDK1045) |
| `dotnet test Commerce.sln -c Release` | **BLOCKED** |
| `npm test` | **PASS** |
| `npm run build` | **PASS** |

---

## 14. Known Limitations

1. **No redirect gateway providers** — ZarinPal/Stripe/PayPal intentionally excluded
2. **No dynamic plugin runtime** — Manual provider registered at compile time in Host
3. **Manual bank transfer** requires admin capture or callback confirmation — no automatic verification
4. **Callback authentication** — generic pipeline; provider-specific HMAC validation deferred to future providers
5. **Backend validation blocked** until .NET 10 SDK available

---

## 15. Architectural Changes

1. Extended `Order` with payment status transition methods and history
2. `Orders.Application` references `Payments.Contracts` only (optional `IPaymentService` for free-order auto-payment)
3. Checkout validation: free vs paid method filtering by `GrandTotal`
4. Separate payment entity status from order `PaymentStatus` enum (richer lifecycle on Payment aggregate)
5. Plugin project pattern established for future gateway providers

---

## 16. Before Phase 18

- Install .NET 10 SDK and run full backend test suite
- Add integration tests: full checkout → order → payment → capture/refund flows
- Implement first redirect provider (e.g. ZarinPal) as separate plugin
- Admin order detail: link to payment detail
- Webhook signature validation per provider
- Payment settings admin UI

---

**Phase 17 complete. Awaiting explicit approval before Phase 18.**
