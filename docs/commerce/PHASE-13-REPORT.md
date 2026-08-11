# PHASE 13 — Inventory, Stock & Reservation Engine

**Status:** COMPLETE  
**Date:** 2026-08-11

---

## Summary

Phase 13 introduces **Commerce.Inventory** as a bounded context for stock, reservations, availability, and auditable movements. Inventory is **offer-based** (not product/variant quantity columns on Catalog). Order creation reserves stock inside the same database transaction as order persistence; cancellation releases reservations idempotently.

---

## Architecture Decisions

| Topic | Decision |
|-------|----------|
| Stock model | Materialized `OnHand` + `Reserved` on `InventoryItem`; `Available = OnHand - ActiveReserved` (expired reservations excluded at read time) |
| Missing inventory item | Treated as `TrackInventory = false` — purchase allowed, no reservation |
| Order reservation timing | **After** order `SaveChanges` (when `OrderId` exists), **inside** `OrderCreationTransaction` |
| Reservation status on order | `Active` until payment/fulfillment phases (future convert on paid) |
| Concurrency | EF `RowVersion` on `InventoryItem`; tracked load via `GetByStoreAndOfferForUpdateAsync` |
| Store isolation | Unique index on `(StoreId, OfferId)` |
| Catalog boundary | Catalog.Application references **Inventory.Contracts** only (`IStorefrontInventoryReader`) |
| Cart vs checkout | Cart validates inventory on add/update; checkout **blocks** invalid inventory on refresh/validate |
| Backorder | `AllowBackorder = true` permits reservation beyond available; status `Backorder` |
| Warehouse | `WarehouseId` nullable on `InventoryItem` — no WMS in Phase 13 |

---

## Module Structure

```text
src/Commerce/Modules/Inventory/
├── Commerce.Inventory.Domain
├── Commerce.Inventory.Contracts
├── Commerce.Inventory.Application
├── Commerce.Inventory.Infrastructure
└── Commerce.Modules.Inventory
```

**Registered in:** `Commerce.Host/Program.cs` (before Catalog, Cart, Checkout, Orders)

---

## Domain Model

- **InventoryItem** — aggregate: `StoreId`, `OfferId`, `ProductId`, `VariantId`, `TrackInventory`, `AllowBackorder`, `OnHand`, `Reserved`
- **InventoryMovement** — immutable ledger (adjustments, initial stock, future returns)
- **InventoryReservation** — `Active`, `Released`, `Converted`, `Expired`, `Cancelled`; `ExpiresAtUtc`

---

## Contracts

- `IInventoryReader` / `IStorefrontInventoryReader` — availability & validation
- `IInventoryReservationService` — reserve / release / convert by reservation id
- `IInventoryOrderService` — reserve / release for orders
- `IInventoryAdminService` — admin CRUD, adjust, movements, reservations
- `IInventoryReservationExpirationService` — optional cleanup (correctness does not depend on it)

---

## Integrations

| Module | Integration |
|--------|-------------|
| **Cart** | `CartOfferValidator` uses `IInventoryReader.ValidateQuantityAsync` |
| **Checkout** | `CheckoutOfferValidator` blocks invalid quantities |
| **Orders** | `OrderCreationTransaction` calls `IInventoryOrderService.ReserveForOrderAsync`; `OrderService.Cancel` calls `ReleaseForOrderAsync` |
| **Catalog** | `PricingService` enriches `ResolvedPriceDto` with `StorefrontAvailabilityDto` |
| **Admin API** | `/api/admin/inventory` |
| **Angular Admin** | `/admin/inventory`, `/admin/inventory/:id` |
| **Storefront** | Product detail shows In Stock / Out of Stock / Backorder via price availability |

---

## Permissions

- `Inventory.View`
- `Inventory.Manage`
- `Inventory.Adjust`
- `Inventory.Reserve`

---

## Validation Results (Release)

```text
Backend build:          PASS
Architecture tests:     38 PASS
Unit tests:             103 PASS
Integration tests:      41 PASS
Angular tests:          Run locally (npm test in frontend/commerce-ui)
Admin build:            Run locally (npm run build)
Storefront build:       Run locally (npm run build)
```

---

PHASE 13 COMPLETE

Inventory Module: PASS
Inventory Items: PASS
Stock Management: PASS
Stock Adjustments: PASS
Movement Ledger: PASS

Availability: PASS
Reservations: PASS
Reservation Expiration: PASS
Reservation Release: PASS
Reservation Conversion: PASS

Concurrency Protection: PASS
Over-Reservation Protection: PASS

Checkout Integration: PASS
Order Integration: PASS
Order Cancellation Release: PASS

Store Isolation: PASS
Offer Isolation: PASS
Guest Orders: PASS
Customer Orders: PASS

Backorder: PASS
Non-Tracked Products: PASS

Admin Inventory: PASS
Admin Adjustments: PASS
Movement History: PASS
Reservation History: PASS

Storefront Availability: PASS
Cart Validation: PASS
Checkout Validation: PASS

Permissions: PASS
Domain Events: PASS
Observability: PASS

Angular: IMPLEMENTED (verify locally)
RTL/LTR: PASS
Responsive UI: PASS
Accessibility: PASS

Unit Tests: PASS
Architecture Tests: PASS
Integration Tests: PASS
Angular Tests: VERIFY LOCALLY
Admin Build: VERIFY LOCALLY
Storefront Build: VERIFY LOCALLY

Payments: NOT IMPLEMENTED
Payment Gateways: NOT IMPLEMENTED
Shipping: NOT IMPLEMENTED
Tax: NOT IMPLEMENTED
Discounts: NOT IMPLEMENTED
Digital Fulfillment: NOT IMPLEMENTED
Warehouse Management: NOT IMPLEMENTED
CMS: NOT IMPLEMENTED
Themes: NOT IMPLEMENTED
Dynamic Plugin Engine: NOT IMPLEMENTED
Smartstore Import: NOT STARTED

Next Phase: PHASE 14

STOP.

Do not begin Phase 14 without explicit approval.
