# PHASE 30 — Shipping & Delivery Engine — Pre-Implementation

**Status:** Pre-implementation  
**Date:** 2026-08-12

---

## Objective

Extend Phase 15 shipping into a provider-independent delivery engine with shipments, tracking, pickup, complete rate types, and checkout integration for digital/physical/mixed carts.

---

## Scope

### In scope

- Shipment + ShipmentItem entities with tracking and lifecycle
- `IShipmentAdminService`, order fulfillment sync
- `IShippingProvider` plugin projects (`Shipping.FlatRate`, `Shipping.Pickup`)
- Weight-based, order-subtotal-based, and quantity-based rates (admin + calculator)
- Pickup methods (`RequiresAddress = false`)
- Resilient provider calculation (failures isolated per provider)
- Checkout: pickup without shipping address; mixed cart behavior preserved
- Admin: shipments, providers list, store settings
- Tests: digital/physical/mixed, zone/rate selection, free shipping, invalid address, provider failure

### Out of scope

- External carrier APIs (FedEx, DHL, etc.)
- Label printing
- Multi-parcel optimization

---

## Architecture

| Component | Role |
|---|---|
| `IShippingProvider` | Plugin contract for rate calculation |
| `IShippingRateProvider` | Checkout bridge |
| `ShippingCalculationService` | Aggregates all providers |
| `ShipmentAdminService` | Creates shipments, tracking, ship/deliver |
| `OrderFulfillmentSync` | Updates order fulfillment status from shipments |
| Plugins | `Commerce.Plugin.Shipping.FlatRate`, `Commerce.Plugin.Shipping.Pickup` |

---

## STOP after Phase 30.
