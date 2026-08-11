# PHASE 12 — Order Engine & Immutable Commercial Snapshots

**Status:** PHASE 12 COMPLETE  
**Date:** 2026-08-11

---

## Summary

Phase 12 converts a validated `ReadyForOrder` checkout into an **immutable commercial Order** with full price, product, address, and customer snapshots. No payments, shipping, inventory, tax rules, or discounts were implemented.

---

## Architecture

### Module layout

```
src/Commerce/Modules/Orders/
├── Commerce.Orders.Domain
├── Commerce.Orders.Contracts
├── Commerce.Orders.Application
├── Commerce.Orders.Infrastructure
└── Commerce.Modules.Orders
```

### Checkout → Order boundary

```
POST /api/orders { checkoutId }
  + Idempotency-Key header
        ↓
ICheckoutOrderPreparationService.ValidateForOrderCreationAsync
        ↓
OrderPreparationResult (server-side prices, product names, addresses)
        ↓
OrderCreationTransaction (atomic: Order + Checkout Completed + Cart Converted)
        ↓
Order + OrderItems + status history
```

The client never submits authoritative prices, totals, product names, or customer identity.

### Order aggregate

| Field group | Purpose |
|---|---|
| Identity | `OrderNumber` (ORD-YYYY-NNNNNN), `StoreId`, `CheckoutId`, `CartId` |
| Customer | `CustomerId`, `GuestEmail`, `CustomerEmail`, `CustomerDisplayName`, `GuestAccessToken` |
| Status | `OrderStatus`, `PaymentStatus`, `FulfillmentStatus` (separate dimensions) |
| Snapshots | `BillingAddress`, `ShippingAddress` (owned value objects) |
| Totals | `Subtotal`, `DiscountTotal`, `ShippingTotal`, `TaxTotal`, `GrandTotal`, `CurrencyCode` |
| Methods | Selected shipping/payment method IDs (snapshot only, no processing) |

**Invariant:** `Subtotal - DiscountTotal + ShippingTotal + TaxTotal = GrandTotal`

### OrderItem snapshots

Each line stores: `ProductName`, `VariantName`, `Sku`, `Quantity`, price fields, `PrimaryImageUrl`, catalog IDs (`OfferId`, `ProductId`, `VariantId`).

Historical display uses order snapshots — not live catalog records.

### Order numbering

Format: `ORD-{year}-{sequence:D6}` (e.g. `ORD-2026-000001`)

- Store-scoped sequence table (`StoreOrderNumberSequence`)
- Unique index on `OrderNumber`
- Generated server-side only

### Idempotency & concurrency

- `Idempotency-Key` header required on `POST /api/orders`
- `OrderCreationIdempotency` table with unique `(StoreId, IdempotencyKey)`
- Unique index on `Order.CheckoutId` — one order per checkout
- `OrderCreationTransaction` uses DB transactions (SQL Server); falls back gracefully for in-memory test provider

### Lifecycle after order creation

| Entity | New state |
|---|---|
| Checkout | `Completed` via `MarkCompleted()` |
| Cart | `Converted` via `MarkConverted()` |

### Status machine

**OrderStatus:** Pending → Confirmed → Processing → Completed | Cancelled  
**PaymentStatus:** Pending (initial) — Paid not set in Phase 12  
**FulfillmentStatus:** Unfulfilled (initial)

Cancellation allowed from Pending/Confirmed/Processing; blocked from Completed/Cancelled.

### Guest orders

- `GuestAccessToken` generated on creation
- Lookup via `GET /api/orders/by-number/{orderNumber}?accessToken=...`
- No order enumeration for guests

### Permissions

| Permission | Purpose |
|---|---|
| `Orders.View` | Admin list/detail |
| `Orders.Manage` | Reserved for future management |
| `Orders.Cancel` | Admin cancellation |

### API endpoints

| Endpoint | Access |
|---|---|
| `POST /api/orders` | Guest or customer |
| `GET /api/orders` | Authenticated customer (own orders) |
| `GET /api/orders/{id}` | Authenticated customer (own order) |
| `GET /api/orders/by-number/{orderNumber}` | Guest (with token) or customer |
| `POST /api/orders/{id}/cancel` | Customer (own order) |
| `GET /api/admin/orders` | Admin |
| `GET /api/admin/orders/{id}` | Admin |
| `POST /api/admin/orders/{id}/cancel` | Admin |

### Angular

| Route | App |
|---|---|
| `/orders`, `/orders/:id` | Admin |
| `/account/orders`, `/account/orders/:id` | Storefront (auth) |
| `/order-confirmation/:orderNumber` | Storefront (guest token in query) |

---

## Validation results

| Check | Result |
|---|---|
| Backend build | PASS (0 errors, 0 warnings) |
| Unit tests | PASS (93) |
| Architecture tests | PASS (33) |
| Integration tests | PASS (36) |
| Angular tests | PASS (4) |
| Admin build | PASS |
| Storefront build | PASS |

---

# PHASE 12 COMPLETE

Order Module: PASS  
Order Aggregate: PASS  
Order Items: PASS  
Order Numbering: PASS  

Checkout → Order: PASS  
Checkout Validation: PASS  
Checkout Conversion: PASS  
Cart Conversion: PASS  

Guest Orders: PASS  
Customer Orders: PASS  
Store Isolation: PASS  
Customer Isolation: PASS  

Price Snapshots: PASS  
Product Snapshots: PASS  
Customer Snapshots: PASS  
Address Snapshots: PASS  
Currency Snapshot: PASS  

Order Totals: PASS  
Order Status: PASS  
Payment Status: PASS  
Fulfillment Status: PASS  
Status History: PASS  

Idempotency: PASS  
Concurrency Protection: PASS  
Transactional Creation: PASS  

Customer Order History: PASS  
Customer Order Detail: PASS  
Guest Confirmation: PASS  

Admin Orders: PASS  
Admin Order Detail: PASS  
Admin Permissions: PASS  
Filtering: PASS  
Pagination: PASS  

Angular: PASS  
RTL/LTR: PASS  
Responsive UI: PASS  
Accessibility: PASS  

Backend Unit Tests: PASS  
Architecture Tests: PASS  
Integration Tests: PASS  
Angular Tests: PASS  
Admin Build: PASS  
Storefront Build: PASS  

Payments: NOT IMPLEMENTED  
Payment Gateways: NOT IMPLEMENTED  
Shipping: NOT IMPLEMENTED  
Inventory: NOT IMPLEMENTED  
Tax Rules: NOT IMPLEMENTED  
Discounts: NOT IMPLEMENTED  
Digital Downloads: NOT IMPLEMENTED  
CMS: NOT IMPLEMENTED  
Themes: NOT IMPLEMENTED  
Dynamic Plugin Engine: NOT IMPLEMENTED  
Smartstore Import: NOT STARTED  

Next Phase: PHASE 13  

STOP.
