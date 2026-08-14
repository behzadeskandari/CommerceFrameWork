# PHASE 31 — Order Fulfillment, Returns & Refunds — Pre-Implementation

**Status:** Complete (pre-implementation)  
**Date:** 2026-08-12

---

## Objective

Create a robust post-order lifecycle with separate status dimensions, financial safety, and integration across Orders, Payments, Inventory, Shipping, and Notifications.

---

## Status Model (unchanged enums, extended)

| Dimension | Enum / entity | Notes |
|---|---|---|
| Order | `OrderStatus` | Added `PartiallyCancelled` |
| Payment | `PaymentStatus` | Existing sync from Payments |
| Fulfillment | `FulfillmentStatus` | Existing; synced from shipments |
| Shipment | `ShipmentStatus` | Shipping module (Phase 30) |
| Return | `ReturnStatus` | New on `ReturnCase` |
| Refund | `RefundStatus` | Payments module on `Refund` entity |

---

## Domain Additions

- `ReturnCase`, `ReturnCaseItem`, `ReturnStatus`, `ReturnResolutionType`
- `OrderItem.CancelledQuantity`, `ReturnedQuantity`, proportional refund calculation
- Order lifecycle: `Confirm`, `MarkProcessing`, `Complete`, `CancelPartial`, `RecordReturn`

---

## Services

| Service | Responsibility |
|---|---|
| `OrderLifecycleService` | Confirm/process/complete, partial cancel, server-side refund orchestration |
| `ReturnCaseService` | Return request → approve/reject → shipment → receive → restock → refund → complete |
| `InventoryOrderService` | `ReleasePartialForOrderAsync`, `RestockForOrderAsync` |
| `PaymentService` | Refund idempotency via `Refund.IdempotencyKey` |
| `ShipmentAdminService` | `CancelOpenShipmentsForOrderAsync` |

---

## Financial Safety

- Refund amounts computed server-side from order lines (never trust client amounts)
- `Idempotency-Key` required for order-level and return-completion refunds
- Refund records and transactions preserved (append-only)
- Provider references preserved on payment/refund transactions

---

## Admin API

| Endpoint | Permission |
|---|---|
| `POST /api/admin/orders/{id}/confirm` | Orders.Manage |
| `POST /api/admin/orders/{id}/processing` | Orders.Manage |
| `POST /api/admin/orders/{id}/complete` | Orders.Manage |
| `POST /api/admin/orders/{id}/partial-cancel` | Orders.Cancel |
| `POST /api/admin/orders/{id}/refund` | Orders.Refund |
| `GET/POST /api/admin/orders/{id}/returns` | Orders.Returns |
| `GET/POST /api/admin/returns/{id}/*` | Orders.Returns |

---

## Tests Planned

- Full/partial refund, duplicate refund (idempotency)
- Cancellation, partial cancellation
- Return workflow
- Inventory release/restock
- Authorization via permissions
- Financial history preservation
