# PHASE 30 — Shipping & Delivery Engine — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## Summary

Phase 30 extends the Phase 15 shipping module into a full delivery engine. Shipment records with tracking integrate with order fulfillment and notifications. Shipping providers implement `IShippingProvider` via plugin projects (FlatRate, Pickup) while the module registers built-in providers for development and tests. Checkout supports pickup without a shipping address, resilient multi-provider calculation, and preserved digital/physical/mixed cart behavior.

---

## Backend Delivered

### Shipment domain

| Entity | Purpose |
|---|---|
| `Shipment` | Order-linked shipment with status, tracking, carrier |
| `ShipmentItem` | Line quantities included in shipment |
| `ShipmentStatus` | Pending, Shipped, Delivered, Cancelled |

### Rate types (complete)

| Type | Factory | Calculator support |
|---|---|---|
| Flat | `CreateFlat` | Base + optional weight surcharge |
| WeightBased | `CreateWeightBased` | Base + per-kg + weight bands |
| OrderSubtotalBased | `CreateOrderSubtotalBased` | Base + subtotal % |
| QuantityBased | `CreateQuantityBased` | Base + per-unit |

Free shipping via `FreeShippingThreshold` unchanged.

### Providers

| Provider | System name | Address required |
|---|---|---|
| Flat rate | `Shipping.FlatRate` | Yes |
| Store pickup | `Shipping.Pickup` | No |

Plugin projects:
- `Commerce.Plugin.Shipping.FlatRate`
- `Commerce.Plugin.Shipping.Pickup`

Both implement `ICommercePlugin` and register `IShippingProvider` + `IShippingRateProvider`.

### Services

| Service | Responsibility |
|---|---|
| `ShipmentAdminService` | CRUD shipments, tracking, ship/deliver |
| `OrderFulfillmentSync` | Syncs `FulfillmentStatus` from shipped quantities |
| `OrderFulfillmentUpdater` | Updates order fulfillment (Orders module) |
| `ShippingProviderRegistry` | Lists registered providers for admin |
| `ShippingCalculationService` | Aggregates providers; isolates failures |

### Checkout integration

- `ShippingOption` / `ShippingOptionDto` include `RequiresAddress`
- Options fetched without address (pickup providers)
- Validation: address required only when selected/all options require it
- Digital-only: no shipping (unchanged)
- Mixed cart: `RequiresShipping = true`; digital lines excluded from rate calc (unchanged)
- Provider failures logged; other providers still return options

### Admin API

| Route | Purpose |
|---|---|
| `GET/POST /api/admin/shipping/shipments` | Shipment management |
| `PUT /api/admin/shipping/shipments/{id}/tracking` | Tracking update |
| `POST .../ship`, `POST .../deliver` | Lifecycle |
| `GET /api/admin/shipping/providers` | Registered providers |
| `GET/PUT /api/admin/shipping/settings` | Store shipping settings |

Existing zones/methods/rates endpoints extended for full rate-type fields.

### Notifications

`MarkShippedAsync` invokes `IShipmentCreatedHandler` (Phase 27 notification handler).

---

## Tests

| Project | Coverage |
|---|---|
| `Commerce.Tests.Unit.Shipping` | Rate types, provider failure isolation, shipment domain |
| `Commerce.Tests.Integration` | Mixed cart, invalid address (no zone), pickup without address, existing Phase 15 flows |

---

## Files of note

```
src/Commerce/Modules/Shipping/Commerce.Shipping.Domain/Entities/Shipment.cs
src/Commerce/Modules/Shipping/Commerce.Shipping.Application/Shipments/
src/Commerce/Modules/Shipping/Commerce.Shipping.Application/Shipping/PickupShippingProvider.cs
src/Commerce/Plugins/Shipping/Commerce.Plugin.Shipping.FlatRate/
src/Commerce/Plugins/Shipping/Commerce.Plugin.Shipping.Pickup/
src/Commerce/Host/Commerce.Host/Shipping/AdminShippingController.cs (extended)
```

---

## Next phase

Phase 31 — not started (per instruction: STOP after Phase 30).
