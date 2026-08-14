# PHASE 29 — Advanced Inventory & Warehouse — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## Summary

Phase 29 extends the Phase 13 inventory module with warehouses, stock locations, multi-warehouse stock rows, incoming quantity, low-stock thresholds, inter-warehouse transfers, reservation-to-sale conversion on payment, and a recurring job to expire stale reservations. All stock changes produce movement records; concurrency is handled via database transactions and row-version locking.

---

## Backend Delivered

### Domain extensions

| Entity / enum | Additions |
|---|---|
| `Warehouse` | Store-scoped warehouse with default/active flags |
| `StockLocation` | Bin/aisle locations within a warehouse |
| `InventoryItem` | `Incoming`, `LowStockThreshold`, `StockLocationId`, transfer/incoming/sale methods |
| `InventoryMovementType` | `Sale`, `TransferOut`, `TransferIn` |
| `InventoryReferenceType` | `Transfer` |

### Contracts

| Interface | Purpose |
|---|---|
| `IWarehouseAdminService` | Warehouse and stock-location CRUD |
| `IInventoryTransferService` | Transfer stock; receive incoming |
| `IInventoryOrderService.ConvertForOrderAsync` | Convert reservations to sales on payment |

Extended DTOs include `Incoming`, `IsLowStock`, warehouse/location IDs, and aggregated offer availability.

### Application services

| Service | Responsibility |
|---|---|
| `WarehouseAdminService` | Warehouse CRUD; creates default stock location |
| `InventoryTransferService` | Atomic transfer + incoming receipt |
| `InventoryWarehouseAllocator` | Multi-warehouse reservation allocation |
| `InventoryOrderService` | Reserve/release/convert with allocator |
| `InventoryReader` | Aggregates availability across warehouses |
| `OrderPaidInventoryHandler` | Converts reservations when order is paid |
| `InventoryReservationExpirationJobHandler` | Releases expired reservations |

### Infrastructure

- `EfInventoryRepository` — warehouse/location queries, pessimistic locking on reserve
- EF configurations for `Warehouse`, `StockLocation`; unique index `(StoreId, OfferId, WarehouseId)`
- `InventoryStartupExtensions.RegisterInventoryRecurringJobsAsync`

### Host API

| Route | Purpose |
|---|---|
| `GET/POST/PUT /api/admin/inventory/warehouses` | Warehouse admin |
| `POST /api/admin/inventory/warehouses/{id}/locations` | Stock locations |
| `POST /api/admin/inventory/transfer` | Inter-warehouse transfer |
| `POST /api/admin/inventory/{id}/receive-incoming` | Receive PO/incoming stock |
| `POST /api/admin/inventory/{id}/low-stock-threshold` | Set threshold |

Existing inventory endpoints remain backward compatible.

### Module registration

- `InventoryModule` depends on `Commerce.Scheduling`
- Registers recurring job `inventory.reservations.expire` (5-minute interval)
- DI: warehouse admin, transfer service, order-paid handler, expiration job handler

---

## Concurrency & audit

- **Reserve:** transaction + reload with lock; rejects oversell when backorder disabled
- **Transfer:** single transaction; source `TransferOut` + destination `TransferIn` movements
- **Adjust / receive:** append-only movements; on-hand never silently overwritten
- **Concurrent reservation test:** two parallel `ReserveAsync` calls with on-hand = 1 → exactly one succeeds

---

## Integration (preserved + extended)

| Flow | Behavior |
|---|---|
| Cart add / update | Validates against aggregated available quantity |
| Checkout validate | Fails if insufficient stock |
| Order create | Multi-warehouse reserve via allocator |
| Order cancel | Releases all active reservations |
| Order paid | Converts reservations; deducts on-hand (sale movement) |

---

## Frontend

| Area | Changes |
|---|---|
| `@commerce/api` | `WarehouseApi`, extended `InventoryApi` (transfer, receive, threshold) |
| Admin | `/inventory/warehouses` list + create; inventory detail shows incoming, warehouse, low-stock forms |
| Nav | Warehouses link under Inventory |

---

## Tests

| Project | Coverage |
|---|---|
| `Commerce.Tests.Unit` | Domain: transfer, incoming, convert-to-sale, low stock (+ existing Phase 13 tests) |
| `Commerce.Tests.Integration` | Warehouse create + transfer, receive incoming, concurrent reservation, overselling |

**Note:** Full solution build currently has unrelated pre-existing errors in Cart, Search, Themes, Plugins, and Orders Infrastructure. The Inventory module projects (`Commerce.Inventory.*`, `Commerce.Modules.Inventory`) build successfully in isolation.

---

## Files of note

```
src/Commerce/Modules/Inventory/
  Commerce.Inventory.Domain/Entities/Warehouse.cs
  Commerce.Inventory.Domain/Entities/StockLocation.cs
  Commerce.Inventory.Application/Inventory/WarehouseAdminService.cs
  Commerce.Inventory.Application/Inventory/InventoryTransferService.cs
  Commerce.Inventory.Application/Integration/OrderPaidInventoryHandler.cs
  Commerce.Inventory.Application/Jobs/InventoryJobHandlers.cs
  Commerce.Inventory.Infrastructure/DependencyInjection/InventoryStartupExtensions.cs
src/Commerce/Host/Commerce.Host/Inventory/AdminWarehousesController.cs
frontend/commerce-ui/libs/api/src/lib/warehouse-api.service.ts
frontend/commerce-ui/apps/admin/src/app/pages/inventory/warehouse-list.page.ts
```

---

## Next phase

Phase 30 — not started (per instruction: STOP after Phase 29).
