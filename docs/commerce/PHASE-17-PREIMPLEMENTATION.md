# PHASE 17 — Payment Engine — Pre-Implementation

**Date:** 2026-08-12

---

## 1. Existing State

### Checkout
- `IPaymentMethodProvider` in `Checkout.Contracts` — method discovery only
- `NoOpPaymentMethodProvider` returns empty list; payment optional when no methods
- Session stores `SelectedPaymentMethodId`, `SelectedPaymentMethodSystemName`
- Validation requires payment method only when methods exist

### Orders
- `PaymentStatus`: Pending, Authorized, Paid, Failed, Refunded, PartiallyRefunded
- No payment domain methods; status never transitions after create
- No payment transaction entities

### Plugin runtime
- Not implemented — compile-time module + separate plugin project pattern (like future dynamic plugins)
- Tax/Shipping use internal provider pattern (`ITaxProvider`, `IShippingProvider`)

---

## 2. Design Decisions

| Topic | Decision |
|---|---|
| Core vs provider | `Commerce.Payments.*` core; `Commerce.Plugin.Payment.Manual` first provider |
| Checkout boundary | Reuse `IPaymentMethodProvider`; Payments module registers `PaymentCheckoutMethodProvider` |
| Processing contract | `IPaymentProvider` in `Payments.Contracts` — no ASP.NET dependency |
| Payment methods | Store-scoped `PaymentMethod` entity (DB config), not hardcoded in core |
| Order flow | Order created first → payment created → provider init → verify/capture |
| Free orders | `GrandTotal == 0` skips method selection; auto free-payment capture |
| Idempotency | `Idempotency-Key` on payment create; unique `OrderId` for primary payment; callback dedup table |
| Sensitive data | Never store CVV, full card, provider secrets in payment entities |
| Order status | Extend `Order` with payment transition methods + history |
| Callback | `POST /api/payments/callback/{provider}` → `IPaymentCallbackHandler` dispatches to provider |

---

## 3. Domain Model

### Payments module entities
- **Payment** — order payment aggregate root
- **PaymentTransaction** — every provider interaction
- **PaymentMethod** — store-scoped method configuration
- **PaymentAttempt** — initiation/retry tracking
- **Refund** — refund request aggregate
- **RefundTransaction** — refund provider interactions
- **PaymentCallbackRecord** — idempotent callback audit

### Enums
- **PaymentStatus** (payment entity): Pending, Initiated, RedirectRequired, Authorized, Captured, Failed, Cancelled, PartiallyRefunded, Refunded
- **PaymentTransactionStatus**: Pending, Succeeded, Failed
- **PaymentTransactionType**: Authorization, Capture, Sale, Void, Refund, PartialRefund, Verification
- **RefundStatus**: Pending, Succeeded, Failed, Cancelled
- **PaymentProviderType**: Manual, Redirect, Hosted, Offline

---

## 4. Payment Pipeline

```text
Checkout (ReadyForOrder, method selected or free)
  → POST /api/orders (idempotent)
  → IPaymentService.CreateForOrderAsync
      → resolve provider from PaymentMethod
      → IPaymentProvider.CreatePaymentAsync
      → Payment + PaymentTransaction + PaymentAttempt
  → PaymentResult (redirect URL / instructions)
  → Callback or admin verify
      → IPaymentProvider.VerifyPaymentAsync
      → update Payment + transactions
      → IOrderPaymentSyncService → Order.MarkPaid / Authorized / Failed
```

---

## 5. Module Dependencies

```text
Commerce.Payments
  → Framework.Core, Framework.Contracts
  → Checkout.Contracts (IPaymentMethodProvider bridge)
  → Orders.Contracts (order payment sync)
  → Store.Contracts (settings)

Commerce.Plugin.Payment.Manual
  → Payments.Contracts only
```

Payments does **not** reference Manual plugin. Host references both.

---

## 6. API Plan

### Storefront
- `POST /api/payments` — create payment for order (Idempotency-Key)
- `GET /api/payments/{id}` — payment status
- `POST /api/payments/callback/{provider}` — provider callback

### Admin
- `GET /api/admin/payments`
- `GET /api/admin/payments/{id}`
- `GET /api/admin/payments/{id}/transactions`
- `POST /api/admin/payments/{id}/capture`
- `POST /api/admin/payments/{id}/void`
- `POST /api/admin/payments/{id}/refund`
- `POST /api/admin/payments/{id}/partial-refund`
- CRUD `/api/admin/payment-methods`

---

## 7. Permissions

- `Payments.View`
- `Payments.Manage`
- `Payments.Refund`
- `Payments.Configure`

---

## 8. Checkout Changes

- Free orders (`GrandTotal == 0`): skip payment method requirement
- Filter out non-free methods when total is zero; inject free method internally
- After order creation, storefront calls payment create API

---

## 9. Angular

### Admin
- `/payments` list, `/payments/:id` detail with transactions/refunds

### Storefront
- Payment step (existing), post-order flow
- `/payment/processing`, `/payment/success`, `/payment/failed`

---

## 10. Out of Scope

- ZarinPal, Stripe, PayPal providers
- Dynamic plugin runtime
- Card vaulting / PCI storage
