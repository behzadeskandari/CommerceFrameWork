# PHASE 29 — Advanced Inventory & Warehouse — Pre-Implementation

**Status:** Pre-implementation  
**Date:** 2026-08-12

---

## Objective

Upgrade inventory from basic single-location stock management to production-grade multi-warehouse inventory with reservations, movements, transfers, incoming stock, low-stock signals, and concurrency-safe purchase integration.

---

## Scope

### In scope

- **Warehouses** and **stock locations** per store
- Quantity dimensions: **on-hand**, **reserved**, **available**, **incoming**
- **Inventory reservations** with expiration job (Phase 28 scheduling)
- **Stock movements** audit trail (adjustments, transfers, sales, receipts)
- **Transfers** between warehouse inventory rows
- **Incoming stock** receipt workflow
- **Low-stock thresholds** and `IsLowStock` flag
- **Backorders** (existing policy, extended for multi-warehouse allocation)
- **Multi-warehouse reservation allocator** for order placement
- **Order paid** handler converts reservations to sales (deduct on-hand)
- Concurrency: pessimistic row locking in repository, transactional reserve/release
- Admin API + UI for warehouses and extended inventory detail
- Unit tests (domain + concurrent reservation) and integration tests (transfer, incoming, overselling)
- Documentation

### Out of scope

- Purchase order module (incoming is manual/admin-driven)
- Pick/pack/ship fulfillment workflows
- Return/refund stock restoration (hook ready via order cancel release; full returns in later phase)
- External WMS integrations

---

## Design decisions

| Topic | Decision |
|---|---|
| Data model | One `InventoryItem` per `(StoreId, OfferId, WarehouseId)` |
| Backward compatibility | Legacy rows with null `WarehouseId` still aggregate; new items auto-assign default warehouse |
| Availability | Aggregated across warehouses for cart/checkout validation |
| Reservation | Allocates from warehouses with available stock (default warehouse first) |
| Sale conversion | On payment: `ConvertReservationToSale` deducts `min(qty, onHand)`; backorder may convert without full deduction |
| Expiration | Recurring job `inventory.reservations.expire` every 5 minutes |
| Negative stock | Prohibited on-hand; backorder allows reservation beyond on-hand |

---

## Integration points

| Module | Integration |
|---|---|
| Cart | `IInventoryReader.ValidateQuantityAsync` (aggregated availability) |
| Checkout | Same validation before order creation |
| Orders | `ReserveForOrderAsync` on create; `ReleaseForOrderAsync` on cancel |
| Payments | `IOrderPaidHandler` → `ConvertForOrderAsync` |
| Scheduling | Recurring reservation expiration job |

---

## STOP after Phase 29.
