# PHASE 31 — Order Fulfillment, Returns & Refunds — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## Summary

Phase 31 implements the post-order lifecycle: order confirmation/processing/completion, partial cancellation, server-side refunds with idempotency, return cases with approval workflow, inventory restock/release, shipment cancellation on order cancel, and notification hooks for return events.

Status dimensions remain separate — order, payment, fulfillment, shipment, return, and refund are not collapsed into a single enum.

---

## Backend Delivered

### Return domain

| Entity / enum | Purpose |
|---|---|
| `ReturnCase` | Return request with resolution type, tracking, refund linkage |
| `ReturnCaseItem` | Line quantities being returned |
| `ReturnStatus` | Requested → Approved → ShipmentPending → Received → Restocked → Refunded → Completed |
| `ReturnResolutionType` | Refund or Replacement |

### Order extensions

| Change | Purpose |
|---|---|
| `OrderStatus.PartiallyCancelled` | Line-level partial cancel without full order cancel |
| `OrderItem.CancelledQuantity` / `ReturnedQuantity` | Track line lifecycle |
| `Order.Confirm/MarkProcessing/Complete` | Admin order progression |
| `Order.CancelPartial` | Partial line cancellation with history |
| `Order.CalculateRefundAmount` | Server-side proportional refund from line totals |
| `OrderStatusHistoryType.Return` | Return audit trail on order |

### Payments — refund idempotency

- `Refund.IdempotencyKey` with unique index per payment
- `IPaymentService.RefundAsync` / `PartialRefundAsync` accept optional idempotency key
- Duplicate key returns existing payment state (no double refund)

### Inventory

| Method | When |
|---|---|
| `ReleasePartialForOrderAsync` | Partial cancel before payment |
| `RestockForOrderAsync` | Cancel/refund/return after sale conversion |
| `InventoryReservation.ReduceQuantity` | Partial reservation release |

### Shipping

- `CancelOpenShipmentsForOrderAsync` cancels Pending/Shipped shipments on full order cancel

### Notifications

New event types: `ReturnRequested`, `ReturnApproved`, `ReturnRejected`, `ReturnCompleted` via `IOrderReturnHandler`.

---

## Admin API

Extended `AdminOrdersController` and new `AdminReturnsController` with lifecycle, refund, and return endpoints. Permissions added: `Orders.Refund`, `Orders.Returns`.

---

## Integration Points

| Module | Integration |
|---|---|
| Orders | Lifecycle orchestration, return cases, line quantities |
| Payments | Refund execution, idempotency, order payment sync (existing) |
| Inventory | Partial release, restock on cancel/refund/return |
| Shipping | Cancel open shipments; fulfillment sync unchanged |
| Notifications | Return event handlers |

---

## Tests

| Test file | Coverage |
|---|---|
| `OrderLifecycleDomainTests.cs` | Confirm/process/complete, partial cancel, refund calc, return workflow, refund idempotency key storage |
| Existing `OrderDomainTests`, `PaymentStateTests` | Regression on cancel/payment states |

Unit test project build may be blocked by unrelated `ThemeRegistry` errors in full solution; Orders/Payments/Inventory/Shipping Application projects build cleanly.

---

## Files (key)

- `Commerce.Orders.Domain/Entities/ReturnCase.cs`
- `Commerce.Orders.Domain/Enums/ReturnEnums.cs`
- `Commerce.Orders.Application/Lifecycle/OrderLifecycleService.cs`
- `Commerce.Orders.Application/Lifecycle/ReturnCaseService.cs`
- `Commerce.Payments.Application/Payments/PaymentService.cs` (idempotency)
- `Commerce.Inventory.Application/Inventory/InventoryOrderService.cs`
- `Commerce.Host/Orders/AdminOrdersController.cs`

---

**Next:** Phase 32 — not started.
