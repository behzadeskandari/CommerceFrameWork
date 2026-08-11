# PHASE 11 — Checkout Engine Report

**Status:** PHASE 11 COMPLETE  
**Date:** 2026-08-11

---

## Summary

Phase 11 implements checkout orchestration from **Cart → validated checkout session → ReadyForOrder**.  
No orders, payments, shipments, tax rules, or discount engines were created.

---

## Architecture

### Module layout

```
src/Commerce/Modules/Checkout/
├── Commerce.Checkout.Domain
├── Commerce.Checkout.Contracts
├── Commerce.Checkout.Application
├── Commerce.Checkout.Infrastructure
└── Commerce.Modules.Checkout
```

### Boundary

| Concept | Role |
|---|---|
| **Cart** | Mutable shopping intent |
| **Checkout** | Validated purchase preparation with price snapshots |
| **Order** (Phase 12) | Immutable commercial transaction |

### Checkout session

`CheckoutSession` aggregate stores ownership, cart linkage, address snapshots, provider selections, totals, and lifecycle state.

**Statuses:** `Active`, `RequiresReview`, `ReadyForOrder`, `Expired`, `Completed`, `Cancelled`  
Phase 11 stops at **`ReadyForOrder`** (never `Completed`).

**Invariant:** one active checkout per cart (unique index on `CartId` for active statuses).

### Ownership

Every operation validates **Store + Cart + (Customer | GuestToken)**. Cross-customer and cross-store access is rejected.

### Cart revalidation

On start/refresh, each cart line is revalidated via Catalog contracts:

- Offer exists, active, store/currency match
- Product published, visible, purchasable
- Price resolved through `ICatalogPricingReader`

### Price change strategy

Each line stores `UnitPrice` (current) and `PreviousUnitPrice` (cart snapshot).  
Mismatch sets `PriceChangeDetected` and moves session to **`RequiresReview`** with a user-facing warning.

### Address snapshots

`CheckoutAddressSnapshot` persists billing/shipping at checkout time (not mutable customer address rows).  
Customers may select owned saved addresses; guests enter addresses inline.

### Provider abstractions (no-op defaults)

| Contract | Phase 11 behavior |
|---|---|
| `IShippingRateProvider` | Not registered — empty shipping options, warning only |
| `ITaxCalculator` | `NoOpTaxCalculator` → tax = 0 |
| `IDiscountCalculator` | `NoOpDiscountCalculator` → discount = 0 |
| `IPaymentMethodProvider` | `NoOpPaymentMethodProvider` → empty methods, warning only |

Shipping method is required only when `RequiresShipping` **and** providers return options.  
Payment method is required only when providers return methods.

### Requires shipping

`CheckoutRequiresShippingEvaluator` returns `false` when all products are `Digital`.

### Totals

Server-side pipeline via `ICheckoutTotalsCalculator` using `Money` — client never supplies authoritative totals.

### Expiration

Setting `Checkout.ExpirationMinutes` (default 60) via Settings module.

### Cart staleness

When `Cart.UpdatedAtUtc` exceeds session `CartUpdatedAtUtc`, checkout becomes **`RequiresReview`**.

### Phase 12 contract

`ICheckoutOrderPreparationService.ValidateForOrderCreationAsync` returns `OrderPreparationResult` for order creation without creating order entities.

---

## API

| Method | Route |
|---|---|
| POST | `/api/checkout` |
| GET | `/api/checkout/{id}` |
| PUT | `/api/checkout/{id}/guest-contact` |
| PUT | `/api/checkout/{id}/billing-address` |
| PUT | `/api/checkout/{id}/shipping-address` |
| GET | `/api/checkout/{id}/shipping-options` |
| PUT | `/api/checkout/{id}/shipping-method` |
| GET | `/api/checkout/{id}/payment-methods` |
| PUT | `/api/checkout/{id}/payment-method` |
| POST | `/api/checkout/{id}/refresh` |
| POST | `/api/checkout/{id}/validate` |

---

## Angular storefront

- Route: `/checkout` with `checkoutCartGuard` (redirects empty cart to `/cart`)
- `CheckoutApi`, `CheckoutStateService`
- Multi-step UI: contact → billing → shipping → shipping method → payment → review
- Steps hidden when irrelevant (digital-only carts skip shipping)
- EN/FA localization, responsive layout, accessible step navigation

---

## Validation results

```
PHASE 11 COMPLETE

Checkout Module: PASS
Checkout Session: PASS
Guest Checkout: PASS
Customer Checkout: PASS

Cart Validation: PASS
Offer Validation: PASS
Price Resolution: PASS
Price Change Detection: PASS

Billing Address: PASS
Shipping Address: PASS
Address Ownership: PASS
Requires Shipping: PASS

Shipping Abstraction: PASS
Tax Abstraction: PASS
Discount Abstraction: PASS
Payment Abstraction: PASS

Checkout Totals: PASS
Currency: PASS
Store Isolation: PASS
Customer Isolation: PASS

Checkout Expiration: PASS
Checkout Refresh: PASS
Checkout Validation: PASS
ReadyForOrder: PASS

Angular Checkout: PASS
Guest Checkout UI: PASS
Customer Checkout UI: PASS
Address UI: PASS
Review UI: PASS
RTL/LTR: PASS
Responsive UI: PASS
Accessibility: PASS

Backend Unit Tests: PASS (81)
Architecture Tests: PASS (30)
Integration Tests: PASS (28)
Angular Tests: PASS (4)
Admin Build: PASS
Storefront Build: PASS

Orders: NOT IMPLEMENTED
Payments: NOT IMPLEMENTED
Shipping: NOT IMPLEMENTED
Tax Rules: NOT IMPLEMENTED
Discounts: NOT IMPLEMENTED
Inventory: NOT IMPLEMENTED
Digital Downloads: NOT IMPLEMENTED
CMS: NOT IMPLEMENTED
Themes: NOT IMPLEMENTED
Plugin Engine: NOT IMPLEMENTED
Smartstore Import: NOT STARTED

Next Phase: PHASE 12

STOP.
```
